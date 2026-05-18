using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Infrastructure.Services.ThirdParty;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class GameMetadataRefreshService
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IEnumerable<IGameMetadataProvider> _providers;
    private readonly GameMetadataOptions _options;
    private readonly ApplicationSettingsService _settings;
    private readonly DocumentService _documentService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GameMetadataRefreshService> _logger;

    public GameMetadataRefreshService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IEnumerable<IGameMetadataProvider> providers,
        IOptions<GameMetadataOptions> options,
        ApplicationSettingsService settings,
        DocumentService documentService,
        IHttpClientFactory httpClientFactory,
        ILogger<GameMetadataRefreshService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _providers = providers;
        _options = options.Value;
        _settings = settings;
        _documentService = documentService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<GameMetadataRefreshResult, FailedResult>> RefreshGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var game = await context.Games
            .Include(item => item.Image)
            .Include(item => item.ExternalIds)
            .FirstOrDefaultAsync(item => item.Id == gameId, cancellationToken);

        if (game is null)
        {
            return new FailedResult("Game not found");
        }

        var needsReleaseDate = game.ReleaseDate is null;
        var needsCover = game.Image is null;
        if (!needsReleaseDate && !needsCover)
        {
            return new GameMetadataRefreshResult(game.Id, game.Name, null, false, false, true, "Release date and cover are already set.");
        }

        var metadata = await GetMetadataAsync(game, cancellationToken);
        if (metadata is null)
        {
            return new GameMetadataRefreshResult(game.Id, game.Name, null, false, false, false, "No metadata found from the configured providers.");
        }

        var updatedReleaseDate = false;
        var updatedCover = false;

        if (needsReleaseDate && metadata.ReleaseDate is not null)
        {
            game.ReleaseDate = metadata.ReleaseDate.Value;
            updatedReleaseDate = true;
        }

        if (needsCover && !string.IsNullOrWhiteSpace(metadata.CoverUrl))
        {
            var cover = await DownloadCoverAsync(game, metadata, cancellationToken);
            if (cover is not null)
            {
                game.Image = cover;
                updatedCover = true;
            }
        }

        if (updatedReleaseDate || updatedCover)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var message = updatedReleaseDate || updatedCover
            ? "Metadata refreshed."
            : "Provider found metadata, but there was no missing release date or cover to apply.";

        return new GameMetadataRefreshResult(game.Id, game.Name, metadata.Provider, updatedReleaseDate, updatedCover, false, message);
    }

    private async Task<GameMetadata?> GetMetadataAsync(Game game, CancellationToken cancellationToken)
    {
        foreach (var provider in await GetProvidersInOrderAsync(cancellationToken))
        {
            var metadata = await provider.SearchAsync(game, cancellationToken);
            if (metadata is not null && (metadata.ReleaseDate is not null || !string.IsNullOrWhiteSpace(metadata.CoverUrl)))
            {
                return metadata;
            }
        }

        return null;
    }

    private async Task<List<IGameMetadataProvider>> GetProvidersInOrderAsync(CancellationToken cancellationToken)
    {
        var mode = await _settings.GetGameMetadataProviderAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(mode))
        {
            mode = _options.Provider;
        }

        var providers = _providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
        var orderedNames = mode.Trim().ToLowerInvariant() switch
        {
            "igdb" => ["IGDB"],
            "rawg" => ["RAWG"],
            "rawgthenigdb" => ["RAWG", "IGDB"],
            _ => new[] { "IGDB", "RAWG" }
        };

        return orderedNames
            .Select(name => providers.GetValueOrDefault(name))
            .Where(provider => provider is not null)
            .Cast<IGameMetadataProvider>()
            .ToList();
    }

    private async Task<MediaDocument?> DownloadCoverAsync(Game game, GameMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(metadata.CoverUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not download {Provider} cover for {Game}. Status: {StatusCode}", metadata.Provider, game.Name, response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var fileName = CreateCoverFileName(game, metadata.CoverUrl!, contentType);
            var result = await _documentService.CreateDocument(stream, fileName, contentType, game);
            return result.Match<MediaDocument?>(document => document, failure =>
            {
                _logger.LogWarning("Could not save {Provider} cover for {Game}: {Errors}", metadata.Provider, game.Name, string.Join("; ", failure.errorMessage));
                return null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not download {Provider} cover for {Game}", metadata.Provider, game.Name);
            return null;
        }
    }

    private static string CreateCoverFileName(Game game, string coverUrl, string? contentType)
    {
        var title = string.Concat(game.Name.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character)).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "game";
        }

        var extension = Path.GetExtension(Uri.TryCreate(coverUrl, UriKind.Absolute, out var uri) ? uri.AbsolutePath : coverUrl);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentType?.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }

        return $"{title} cover{extension}";
    }
}

public sealed record GameMetadataRefreshResult(
    int GameId,
    string GameName,
    string? Provider,
    bool UpdatedReleaseDate,
    bool UpdatedCover,
    bool Skipped,
    string Message);
