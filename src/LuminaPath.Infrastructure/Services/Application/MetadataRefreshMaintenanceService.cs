using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class MetadataRefreshMaintenanceService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly GameMetadataRefreshService _gameMetadataRefreshService;

    public MetadataRefreshMaintenanceService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        GameMetadataRefreshService gameMetadataRefreshService)
    {
        _dbContextFactory = dbContextFactory;
        _gameMetadataRefreshService = gameMetadataRefreshService;
    }

    public async Task<MetadataRefreshMaintenanceResult> RefreshMissingAsync(
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        return NormalizeMediaType(mediaType) switch
        {
            MetadataRefreshMediaTypes.Games => await RefreshMissingGamesAsync(cancellationToken),
            MetadataRefreshMediaTypes.Animes => await ReportUnsupportedAsync<Anime>(MetadataRefreshMediaTypes.Animes, "anime", cancellationToken),
            MetadataRefreshMediaTypes.Movies => await ReportUnsupportedAsync<Movie>(MetadataRefreshMediaTypes.Movies, "movie", cancellationToken),
            MetadataRefreshMediaTypes.Series => await ReportUnsupportedAsync<Series>(MetadataRefreshMediaTypes.Series, "series", cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, "Unsupported metadata refresh media type.")
        };
    }

    private async Task<MetadataRefreshMaintenanceResult> RefreshMissingGamesAsync(CancellationToken cancellationToken)
    {
        List<int> candidateIds;
        await using (var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            candidateIds = await context.Games
                .AsNoTracking()
                .Where(game => game.ReleaseDate == null || game.Image == null)
                .OrderBy(game => game.Name)
                .Select(game => game.Id)
                .ToListAsync(cancellationToken);
        }

        var updatedReleaseDates = 0;
        var updatedImages = 0;
        var refreshed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var gameId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _gameMetadataRefreshService.RefreshGameAsync(gameId, cancellationToken);
            result.Match(
                refresh =>
                {
                    if (refresh.UpdatedReleaseDate)
                    {
                        updatedReleaseDates++;
                    }

                    if (refresh.UpdatedCover)
                    {
                        updatedImages++;
                    }

                    if (refresh.UpdatedReleaseDate || refresh.UpdatedCover)
                    {
                        refreshed++;
                    }
                    else
                    {
                        skipped++;
                    }

                    return 0;
                },
                failure =>
                {
                    failed++;
                    return 0;
                });
        }

        var message = candidateIds.Count == 0
            ? "No games are missing release dates or cover images."
            : $"Scanned {candidateIds.Count} game(s), refreshed {refreshed}.";

        return new MetadataRefreshMaintenanceResult(
            MetadataRefreshMediaTypes.Games,
            candidateIds.Count,
            refreshed,
            updatedReleaseDates,
            updatedImages,
            skipped,
            failed,
            message);
    }

    private async Task<MetadataRefreshMaintenanceResult> ReportUnsupportedAsync<TMedia>(
        string mediaType,
        string label,
        CancellationToken cancellationToken)
        where TMedia : Media
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var candidates = await context.Set<TMedia>()
            .AsNoTracking()
            .CountAsync(media => media.ReleaseDate == null || media.Image == null, cancellationToken);

        return new MetadataRefreshMaintenanceResult(
            mediaType,
            candidates,
            Refreshed: 0,
            UpdatedReleaseDates: 0,
            UpdatedImages: 0,
            Skipped: candidates,
            Failed: 0,
            Message: $"Found {candidates} {label} item(s) missing metadata. No {label} metadata provider is implemented yet.");
    }

    private static string NormalizeMediaType(string mediaType)
    {
        return mediaType.Trim().ToLowerInvariant() switch
        {
            "game" or "games" => MetadataRefreshMediaTypes.Games,
            "anime" or "animes" => MetadataRefreshMediaTypes.Animes,
            "movie" or "movies" => MetadataRefreshMediaTypes.Movies,
            "series" => MetadataRefreshMediaTypes.Series,
            _ => mediaType
        };
    }
}

public static class MetadataRefreshMediaTypes
{
    public const string Games = "games";
    public const string Animes = "animes";
    public const string Movies = "movies";
    public const string Series = "series";
}

public sealed record MetadataRefreshMaintenanceResult(
    string MediaType,
    int Candidates,
    int Refreshed,
    int UpdatedReleaseDates,
    int UpdatedImages,
    int Skipped,
    int Failed,
    string Message);
