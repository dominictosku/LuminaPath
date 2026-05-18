using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.ThirdParty;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Test.Utilities;

namespace Test.Services;

public class GameMetadataRefreshServiceTests
{
    [Fact]
    public async Task RefreshGameAsync_FillsMissingReleaseDate()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.Games.Add(new Game { Name = "Celeste" });
            await context.SaveChangesAsync();
        }

        var releaseDate = new DateTime(2018, 1, 25);
        var service = CreateService(options, new FakeMetadataProvider("IGDB", new GameMetadata("IGDB", releaseDate, null)));

        var result = await service.RefreshGameAsync(1);
        var refresh = result.Match(success => success, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.True(refresh.UpdatedReleaseDate);
        Assert.False(refresh.UpdatedCover);
        Assert.Equal("IGDB", refresh.Provider);

        await using var assertContext = new LuminaPathDbContext(options);
        var game = await assertContext.Games.SingleAsync();
        Assert.Equal(releaseDate, game.ReleaseDate);
    }

    [Fact]
    public async Task RefreshGameAsync_DoesNotOverwriteManualReleaseDateOrCover()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var manualReleaseDate = new DateTime(2020, 2, 3);
        await using (var context = new LuminaPathDbContext(options))
        {
            context.Games.Add(new Game
            {
                Name = "Manual Game",
                ReleaseDate = manualReleaseDate,
                Image = new MediaDocument
                {
                    Name = "manual.png",
                    StorageName = "manual-storage.png",
                    ContentType = "image/png"
                }
            });
            await context.SaveChangesAsync();
        }

        var service = CreateService(options, new FakeMetadataProvider("IGDB", new GameMetadata("IGDB", new DateTime(2024, 1, 1), "https://example.com/cover.jpg")));

        var result = await service.RefreshGameAsync(1);
        var refresh = result.Match(success => success, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));

        Assert.True(refresh.Skipped);
        Assert.False(refresh.UpdatedReleaseDate);
        Assert.False(refresh.UpdatedCover);

        await using var assertContext = new LuminaPathDbContext(options);
        var game = await assertContext.Games.Include(item => item.Image).SingleAsync();
        Assert.Equal(manualReleaseDate, game.ReleaseDate);
        Assert.Equal("manual-storage.png", game.Image?.StorageName);
    }

    private static GameMetadataRefreshService CreateService(
        DbContextOptions<LuminaPathDbContext> options,
        params IGameMetadataProvider[] providers)
    {
        var factory = new TestDbContextFactory(options);
        var settings = new ApplicationSettingsService(factory);
        var documentService = new DocumentService(
            factory,
            new Mock<IStorageService>().Object,
            new Mock<ILogger<DocumentService>>().Object);

        return new GameMetadataRefreshService(
            factory,
            providers,
            Options.Create(new GameMetadataOptions { Provider = GameMetadataProviderModes.IgdbThenRawg }),
            settings,
            documentService,
            new Mock<IHttpClientFactory>().Object,
            new Mock<ILogger<GameMetadataRefreshService>>().Object);
    }

    private sealed class FakeMetadataProvider : IGameMetadataProvider
    {
        private readonly GameMetadata? _metadata;

        public FakeMetadataProvider(string name, GameMetadata? metadata)
        {
            Name = name;
            _metadata = metadata;
        }

        public string Name { get; }

        public Task<GameMetadata?> SearchAsync(Game game, CancellationToken cancellationToken)
        {
            return Task.FromResult(_metadata);
        }
    }
}
