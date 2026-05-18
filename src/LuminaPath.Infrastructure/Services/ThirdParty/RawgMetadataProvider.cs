using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class RawgMetadataProvider : IGameMetadataProvider
{
    public string Name => "RAWG";

    private readonly HttpClient _http;
    private readonly GameMetadataOptions _options;
    private readonly ApplicationSettingsService _settings;
    private readonly ILogger<RawgMetadataProvider> _logger;

    public RawgMetadataProvider(
        HttpClient http,
        IOptions<GameMetadataOptions> options,
        ApplicationSettingsService settings,
        ILogger<RawgMetadataProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _settings = settings;
        _logger = logger;
    }

    public async Task<GameMetadata?> SearchAsync(Game game, CancellationToken cancellationToken)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var externalId = game.ExternalIds.GetExternalId(ExternalMediaProvider.Rawg);
        var url = string.IsNullOrWhiteSpace(externalId)
            ? $"{_options.RawgBaseUrl.TrimEnd('/')}/games?key={Uri.EscapeDataString(apiKey)}&search={Uri.EscapeDataString(game.Name)}&page_size=1"
            : $"{_options.RawgBaseUrl.TrimEnd('/')}/games/{Uri.EscapeDataString(externalId)}?key={Uri.EscapeDataString(apiKey)}";

        try
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                var search = await _http.GetFromJsonAsync<RawgSearchResponse>(url, cancellationToken);
                var match = search?.Results.FirstOrDefault();
                return match is null ? null : ToMetadata(match);
            }

            var details = await _http.GetFromJsonAsync<RawgGame>(url, cancellationToken);
            return details is null ? null : ToMetadata(details);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAWG metadata lookup failed for {Game}", game.Name);
            return null;
        }
    }

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        var saved = await _settings.GetRawgApiKeyAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(saved) ? _options.RawgApiKey : saved;
    }

    private GameMetadata ToMetadata(RawgGame game)
    {
        return new GameMetadata(
            Name,
            DateTime.TryParse(game.Released, out var releaseDate) ? releaseDate.Date : null,
            game.BackgroundImage);
    }

    private sealed class RawgSearchResponse
    {
        [JsonPropertyName("results")]
        public List<RawgGame> Results { get; set; } = new();
    }

    private sealed class RawgGame
    {
        [JsonPropertyName("released")]
        public string? Released { get; set; }

        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }
    }
}
