using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;

namespace Test.Services;

public class MediaAuditTests
{
    [Fact]
    public async Task GameService_RecordsMediaAuditForCreateUpdateAndDelete()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var factory = new TestDbContextFactory(options);
        var service = CreateGameService(options, factory);

        var createdResult = await service.PostAsync(new Game
        {
            Name = "Hades",
            Description = "Original",
            Genres = ["Action"],
            Source = "Manual"
        });
        var game = createdResult.Match(success => success, failure => throw new InvalidOperationException(string.Join(", ", failure.errorMessage)));

        game.Description = "Updated";
        game.Genres = ["Action", "Roguelike"];
        await service.PutAsync(game);
        await service.DeleteAsync(game.Id);

        await using var assertContext = new LuminaPathDbContext(options);
        var auditLogs = await assertContext.AuditLogs
            .Where(log => log.Category == AuditCategories.Media)
            .OrderBy(log => log.Id)
            .ToListAsync();

        Assert.Collection(
            auditLogs,
            log =>
            {
                Assert.Equal(AuditActions.MediaCreated, log.Action);
                Assert.Equal(AuditOutcomes.Success, log.Outcome);
                Assert.Equal(nameof(Game), log.TargetType);
                Assert.Equal("Hades", log.TargetName);
            },
            log =>
            {
                Assert.Equal(AuditActions.MediaUpdated, log.Action);
                Assert.Equal(AuditOutcomes.Success, log.Outcome);
                Assert.Contains("Description", log.ChangesJson);
                Assert.Contains("Updated", log.ChangesJson);
            },
            log =>
            {
                Assert.Equal(AuditActions.MediaDeleted, log.Action);
                Assert.Equal(AuditOutcomes.Success, log.Outcome);
                Assert.Equal("Hades", log.TargetName);
            });
    }

    [Fact]
    public async Task GameService_RecordsFailedMediaCreateAuditForDuplicateName()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var dbContext = new LuminaPathDbContext(options))
        {
            dbContext.Games.Add(new Game { Name = "Hades" });
            await dbContext.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var service = CreateGameService(options, factory);

        var result = await service.PostAsync(new Game { Name = "Hades" });

        Assert.True(result.IsError);
        await using var assertContext = new LuminaPathDbContext(options);
        var auditLog = Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == AuditActions.MediaCreated)
            .ToListAsync());
        Assert.Equal(AuditCategories.Media, auditLog.Category);
        Assert.Equal(AuditOutcomes.Failure, auditLog.Outcome);
        Assert.Equal(nameof(Game), auditLog.TargetType);
        Assert.Contains("Title is already registered", auditLog.ErrorMessage);
    }

    private static GameService CreateGameService(
        DbContextOptions<LuminaPathDbContext> options,
        TestDbContextFactory factory)
    {
        using var dbContext = new LuminaPathDbContext(options);
        var mapper = dbContext.GetService<IObjectMapper>();
        var documentService = new DocumentService(
            factory,
            new Mock<IStorageService>().Object,
            new Mock<ILogger<DocumentService>>().Object);

        return new GameService(
            factory,
            documentService,
            mapper,
            new AuditLogService(factory));
    }
}
