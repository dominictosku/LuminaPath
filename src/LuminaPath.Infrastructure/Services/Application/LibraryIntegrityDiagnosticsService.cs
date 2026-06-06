using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class LibraryIntegrityDiagnosticsService
{
    private const int DetailLimit = 50;
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public LibraryIntegrityDiagnosticsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<LibraryIntegrityDiagnosticsResult> RunAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var externalIds = await context.MediaExternalIds
            .AsNoTracking()
            .Include(externalId => externalId.Media)
            .Where(externalId => externalId.ExternalId != string.Empty)
            .ToListAsync(cancellationToken);

        var duplicateExternalIdDetails = externalIds
            .GroupBy(externalId => new { externalId.Provider, externalId.ExternalId })
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.Provider)
            .ThenBy(group => group.Key.ExternalId)
            .Take(DetailLimit)
            .Select(group => new DuplicateExternalIdDiagnostic(
                Provider: group.Key.Provider.ToString(),
                ExternalId: group.Key.ExternalId,
                Count: group.Count(),
                Media: group
                    .OrderBy(externalId => externalId.MediaId)
                    .Select(externalId => new DuplicateExternalIdMediaDiagnostic(
                        ExternalIdRowId: externalId.Id,
                        MediaId: externalId.MediaId,
                        MediaType: GetMediaType(externalId.Media),
                        MediaName: externalId.Media?.Name ?? "(missing media row)"))
                    .ToList()))
            .ToList();

        var duplicateExternalIds = externalIds
            .GroupBy(externalId => new { externalId.Provider, externalId.ExternalId })
            .Count(group => group.Count() > 1);

        var missingGameLinks = await context.MyGames
            .AsNoTracking()
            .Where(entry => !context.Games.Any(media => media.Id == entry.GameId))
            .Select(entry => new BrokenLibraryReferenceDiagnostic("Game", entry.Id, entry.GameId, entry.LuminaUserId))
            .ToListAsync(cancellationToken);
        var missingAnimeLinks = await context.MyAnimes
            .AsNoTracking()
            .Where(entry => !context.Animes.Any(media => media.Id == entry.AnimeId))
            .Select(entry => new BrokenLibraryReferenceDiagnostic("Anime", entry.Id, entry.AnimeId, entry.LuminaUserId))
            .ToListAsync(cancellationToken);
        var missingMovieLinks = await context.MyMovies
            .AsNoTracking()
            .Where(entry => !context.Movies.Any(media => media.Id == entry.MovieId))
            .Select(entry => new BrokenLibraryReferenceDiagnostic("Movie", entry.Id, entry.MovieId, entry.LuminaUserId))
            .ToListAsync(cancellationToken);
        var missingSeriesLinks = await context.MySeries
            .AsNoTracking()
            .Where(entry => !context.Series.Any(media => media.Id == entry.SeriesId))
            .Select(entry => new BrokenLibraryReferenceDiagnostic("Series", entry.Id, entry.SeriesId, entry.LuminaUserId))
            .ToListAsync(cancellationToken);

        var brokenReferenceDetails = missingGameLinks
            .Concat(missingAnimeLinks)
            .Concat(missingMovieLinks)
            .Concat(missingSeriesLinks)
            .OrderBy(item => item.MediaType)
            .ThenBy(item => item.LibraryEntryId)
            .Take(DetailLimit)
            .ToList();

        var documentsMissingStorageDetails = await context.Documents
            .AsNoTracking()
            .Where(document => document.StorageName == null || document.StorageName == string.Empty)
            .OrderBy(document => document.Id)
            .Take(DetailLimit)
            .Select(document => new DocumentMissingStorageDiagnostic(
                document.Id,
                document.Name ?? "(unnamed)",
                document.DocumentType.ToString()))
            .ToListAsync(cancellationToken);

        var documentsMissingStorage = await context.Documents
            .AsNoTracking()
            .Where(document => document.StorageName == null || document.StorageName == string.Empty)
            .CountAsync(cancellationToken);

        return new LibraryIntegrityDiagnosticsResult(
            duplicateExternalIds,
            missingGameLinks.Count + missingAnimeLinks.Count + missingMovieLinks.Count + missingSeriesLinks.Count,
            documentsMissingStorage,
            duplicateExternalIdDetails,
            brokenReferenceDetails,
            documentsMissingStorageDetails);
    }

    private static string GetMediaType(Media? media)
    {
        return media switch
        {
            Game => "Game",
            Anime => "Anime",
            Movie => "Movie",
            Series => "Series",
            null => "Missing",
            _ => media.GetType().Name
        };
    }
}

public sealed record LibraryIntegrityDiagnosticsResult(
    int DuplicateExternalIdGroups,
    int LibraryEntriesMissingMedia,
    int DocumentsMissingStorageName,
    IReadOnlyList<DuplicateExternalIdDiagnostic> DuplicateExternalIds,
    IReadOnlyList<BrokenLibraryReferenceDiagnostic> BrokenReferences,
    IReadOnlyList<DocumentMissingStorageDiagnostic> DocumentsMissingStorage)
{
    public int TotalIssues => DuplicateExternalIdGroups + LibraryEntriesMissingMedia + DocumentsMissingStorageName;
}

public sealed record DuplicateExternalIdDiagnostic(
    string Provider,
    string ExternalId,
    int Count,
    IReadOnlyList<DuplicateExternalIdMediaDiagnostic> Media);

public sealed record DuplicateExternalIdMediaDiagnostic(
    int ExternalIdRowId,
    int MediaId,
    string MediaType,
    string MediaName);

public sealed record BrokenLibraryReferenceDiagnostic(
    string MediaType,
    int LibraryEntryId,
    int MissingMediaId,
    string UserId);

public sealed record DocumentMissingStorageDiagnostic(
    int DocumentId,
    string Name,
    string DocumentType);
