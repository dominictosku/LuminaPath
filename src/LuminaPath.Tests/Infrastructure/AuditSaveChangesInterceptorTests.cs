using System.Security.Claims;
using System.Text.Json;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Infrastructure;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task SaveChanges_AddsFallbackAudit_ForSensitiveSettingChange()
    {
        using var provider = CreateServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = ApplicationSettingsService.SteamApiKey,
                Value = "old-secret"
            });
            await context.SaveChangesAsync();

            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            var setting = await context.ApplicationSettings.SingleAsync();
            setting.Value = "new-secret";
            await context.SaveChangesAsync();
        }

        await using var assertContext = await factory.CreateDbContextAsync();
        var auditLog = await assertContext.AuditLogs.SingleAsync();
        Assert.Equal(AuditCategories.System, auditLog.Category);
        Assert.Equal(AuditActions.EntityChanged, auditLog.Action);
        Assert.Equal(nameof(ApplicationSetting), auditLog.TargetType);
        Assert.Equal(ApplicationSettingsService.SteamApiKey, auditLog.TargetId);
        Assert.Equal("admin-id", auditLog.ActorUserId);
        Assert.Equal("/Admin/Settings", auditLog.RequestPath);
        Assert.DoesNotContain("old-secret", auditLog.ChangesJson);
        Assert.DoesNotContain("new-secret", auditLog.ChangesJson);

        using var changes = JsonDocument.Parse(auditLog.ChangesJson!);
        var valueChange = changes.RootElement.GetProperty(nameof(ApplicationSetting.Value));
        Assert.Equal("set", valueChange.GetProperty("old").GetString());
        Assert.Equal("set", valueChange.GetProperty("new").GetString());
    }

    [Fact]
    public async Task SaveChanges_DoesNotCreateFallbackAudit_WhenOnlyAuditLogChanges()
    {
        using var provider = CreateServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.AuditLogs.Add(new AuditLog
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.UserCreated,
                Outcome = AuditOutcomes.Success,
                TimestampUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await using var assertContext = await factory.CreateDbContextAsync();
        var auditLog = await assertContext.AuditLogs.SingleAsync();
        Assert.Equal(AuditActions.UserCreated, auditLog.Action);
    }

    [Fact]
    public async Task SaveChanges_RedactsPasswordHashState_ForUserChanges()
    {
        using var provider = CreateServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.Add(new LuminaPath.Infrastructure.Identity.LuminaUser
            {
                Id = "user-id",
                Email = "user@example.test",
                UserName = "user@example.test",
                PasswordHash = "old-hash"
            });
            await context.SaveChangesAsync();

            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            var user = await context.Users.SingleAsync();
            user.PasswordHash = "new-hash";
            await context.SaveChangesAsync();
        }

        await using var assertContext = await factory.CreateDbContextAsync();
        var auditLog = await assertContext.AuditLogs.SingleAsync();
        Assert.Equal(nameof(LuminaPath.Infrastructure.Identity.LuminaUser), auditLog.TargetType);
        Assert.DoesNotContain("old-hash", auditLog.ChangesJson);
        Assert.DoesNotContain("new-hash", auditLog.ChangesJson);
        Assert.Contains(nameof(LuminaPath.Infrastructure.Identity.LuminaUser.PasswordHash), auditLog.ChangesJson);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContext()
        };
        var databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddDbContextFactory<LuminaPathDbContext>((serviceProvider, options) =>
            options
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Email, "admin@example.test")
            ], "UnitTest"))
        };

        context.Request.Path = "/Admin/Settings";
        context.Request.Method = "POST";
        return context;
    }
}
