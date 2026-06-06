using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class LibraryIntegrityDiagnosticsService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public LibraryIntegrityDiagnosticsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<LibraryIntegrityDiagnosticsResult> RunAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var duplicateExternalIds = await context.MediaExternalIds
            .AsNoTracking()
            .Where(externalId => externalId.ExternalId != string.Empty)
            .GroupBy(externalId => new { externalId.Provider, externalId.ExternalId })
            .Where(group => group.Count() > 1)
            .CountAsync(cancellationToken);

        var missingGameLinks = await context.MyGames
            .AsNoTracking()
            .CountAsync(entry => !context.Games.Any(media => media.Id == entry.GameId), cancellationToken);
        var missingAnimeLinks = await context.MyAnimes
            .AsNoTracking()
            .CountAsync(entry => !context.Animes.Any(media => media.Id == entry.AnimeId), cancellationToken);
        var missingMovieLinks = await context.MyMovies
            .AsNoTracking()
            .CountAsync(entry => !context.Movies.Any(media => media.Id == entry.MovieId), cancellationToken);
        var missingSeriesLinks = await context.MySeries
            .AsNoTracking()
            .CountAsync(entry => !context.Series.Any(media => media.Id == entry.SeriesId), cancellationToken);

        var documentsMissingStorage = await context.Documents
            .AsNoTracking()
            .CountAsync(document => document.StorageName == null || document.StorageName == string.Empty, cancellationToken);

        return new LibraryIntegrityDiagnosticsResult(
            duplicateExternalIds,
            missingGameLinks + missingAnimeLinks + missingMovieLinks + missingSeriesLinks,
            documentsMissingStorage);
    }
}

public sealed record LibraryIntegrityDiagnosticsResult(
    int DuplicateExternalIdGroups,
    int LibraryEntriesMissingMedia,
    int DocumentsMissingStorageName)
{
    public int TotalIssues => DuplicateExternalIdGroups + LibraryEntriesMissingMedia + DocumentsMissingStorageName;
}
