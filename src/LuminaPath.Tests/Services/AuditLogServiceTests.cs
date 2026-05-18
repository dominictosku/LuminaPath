using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Test.Utilities;

namespace Test.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task RecordAsync_CapturesActorAndRequestMetadata()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var accessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContext()
        };
        var service = new AuditLogService(
            new TestDbContextFactory(options),
            accessor,
            new Mock<ILogger<AuditLogService>>().Object);

        await service.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Admin,
            Action = AuditActions.UserDeleted,
            TargetType = "User",
            TargetId = "target-id",
            TargetName = "target@example.test",
            Metadata = new { source = "Test" }
        });

        await using var context = new LuminaPathDbContext(options);
        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal("admin-id", auditLog.ActorUserId);
        Assert.Equal("admin@example.test", auditLog.ActorEmail);
        Assert.Equal("/Admin/Users", auditLog.RequestPath);
        Assert.Equal("POST", auditLog.HttpMethod);
        Assert.Equal("unit-test-agent", auditLog.UserAgent);
        Assert.Contains("\"source\":\"Test\"", auditLog.MetadataJson);
    }

    [Fact]
    public async Task SearchAsync_FiltersSortsAndPaginates()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.AuditLogs.AddRange(
                CreateLog("Login", "Failure", "first@example.test", new DateTime(2026, 5, 18, 9, 0, 0, DateTimeKind.Utc)),
                CreateLog("Login", "Failure", "second@example.test", new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc)),
                CreateLog("Logout", "Success", "third@example.test", new DateTime(2026, 5, 18, 11, 0, 0, DateTimeKind.Utc)));
            await context.SaveChangesAsync();
        }

        var service = new AuditLogService(new TestDbContextFactory(options));

        var result = await service.SearchAsync(new AuditLogQuery(
            Category: AuditCategories.Account,
            Action: AuditActions.Login,
            Outcome: AuditOutcomes.Failure,
            Search: "example.test",
            Page: 0,
            PageSize: 1));

        Assert.Equal(2, result.Total);
        var auditLog = Assert.Single(result.Items);
        Assert.Equal("second@example.test", auditLog.TargetName);
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

        context.Request.Path = "/Admin/Users";
        context.Request.Method = "POST";
        context.Request.Headers.UserAgent = "unit-test-agent";
        context.TraceIdentifier = "trace-id";
        return context;
    }

    private static AuditLog CreateLog(string action, string outcome, string targetName, DateTime timestamp)
    {
        return new AuditLog
        {
            TimestampUtc = timestamp,
            Category = AuditCategories.Account,
            Action = action,
            Outcome = outcome,
            TargetType = "User",
            TargetName = targetName,
            RequestPath = "/Account/Login"
        };
    }
}
