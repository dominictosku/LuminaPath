using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services;

public class ApplicationSettingsServiceTests
{
    [Fact]
    public async Task SaveSteamApiKeyAsync_PersistsSettingAndWritesRedactedAudit()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var factory = new TestDbContextFactory(options);
        var service = new ApplicationSettingsService(factory, new AuditLogService(factory));

        await service.SaveSteamApiKeyAsync(" secret-key ");

        await using var context = new LuminaPathDbContext(options);
        var setting = await context.ApplicationSettings.SingleAsync();
        Assert.Equal(ApplicationSettingsService.SteamApiKey, setting.Key);
        Assert.Equal("secret-key", setting.Value);

        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal(AuditActions.ApplicationSettingChanged, auditLog.Action);
        Assert.Equal(ApplicationSettingsService.SteamApiKey, auditLog.TargetId);
        Assert.Contains("\"old\":\"empty\"", auditLog.ChangesJson);
        Assert.Contains("\"new\":\"set\"", auditLog.ChangesJson);
        Assert.DoesNotContain("secret-key", auditLog.ChangesJson);
    }

    [Fact]
    public async Task SaveValueAsync_DoesNotWriteAudit_WhenValueIsUnchanged()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var factory = new TestDbContextFactory(options);
        var service = new ApplicationSettingsService(factory, new AuditLogService(factory));

        await service.SaveNewsEnabledAsync(true);
        await service.SaveNewsEnabledAsync(true);

        await using var context = new LuminaPathDbContext(options);
        Assert.Equal("True", await context.ApplicationSettings
            .Where(setting => setting.Key == ApplicationSettingsService.NewsEnabled)
            .Select(setting => setting.Value)
            .SingleAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task SaveGameMetadataSettingsAsync_UpdatesExistingValues()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.MetadataProvider, Value = "Rawg" },
                new ApplicationSetting { Key = ApplicationSettingsService.MetadataIgdbClientId, Value = "old-client" });
            await context.SaveChangesAsync();
        }

        var service = new ApplicationSettingsService(new TestDbContextFactory(options));

        await service.SaveGameMetadataSettingsAsync(new GameMetadataSettings(
            "Igdb",
            "new-client",
            "new-secret",
            "rawg-key"));

        await using var assertContext = new LuminaPathDbContext(options);
        var settings = await assertContext.ApplicationSettings.ToDictionaryAsync(setting => setting.Key, setting => setting.Value);
        Assert.Equal("Igdb", settings[ApplicationSettingsService.MetadataProvider]);
        Assert.Equal("new-client", settings[ApplicationSettingsService.MetadataIgdbClientId]);
        Assert.Equal("new-secret", settings[ApplicationSettingsService.MetadataIgdbClientSecret]);
        Assert.Equal("rawg-key", settings[ApplicationSettingsService.MetadataRawgApiKey]);
    }
}
