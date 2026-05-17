using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Third_Party;

public sealed class IgdbMetadataProvider : IGameMetadataProvider
{
    public string Name => "IGDB";

    private readonly HttpClient _http;
    private readonly GameMetadataOptions _options;
    private readonly ApplicationSettingsService _settings;
    private readonly ILogger<IgdbMetadataProvider> _logger;
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public IgdbMetadataProvider(
        HttpClient http,
        IOptions<GameMetadataOptions> options,
        ApplicationSettingsService settings,
        ILogger<IgdbMetadataProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _settings = settings;
        _logger = logger;
    }

    public async Task<GameMetadata?> SearchAsync(Game game, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var token = await GetAccessTokenAsync(clientId, cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var query = CreateGameQuery(game);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.IgdbBaseUrl.TrimEnd('/')}/games")
        {
            Content = new StringContent(query, Encoding.UTF8, "text/plain")
        };
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IGDB metadata lookup failed for {Game}. Status: {StatusCode}", game.Name, response.StatusCode);
                return null;
            }

            var results = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(cancellationToken: cancellationToken);
            var match = results?.FirstOrDefault();
            if (match is null)
            {
                return null;
            }

            return new GameMetadata(
                Name,
                match.FirstReleaseDate is null
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(match.FirstReleaseDate.Value).UtcDateTime.Date,
                NormalizeCoverUrl(match.Cover?.Url));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IGDB metadata lookup failed for {Game}", game.Name);
            return null;
        }
    }

    private async Task<string> GetClientIdAsync(CancellationToken cancellationToken)
    {
        var saved = await _settings.GetIgdbClientIdAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(saved) ? _options.IgdbClientId : saved;
    }

    private async Task<string?> GetAccessTokenAsync(string clientId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _accessToken;
        }

        var clientSecret = await _settings.GetIgdbClientSecretAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            clientSecret = _options.IgdbClientSecret;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var tokenUrl = $"{_options.TwitchTokenUrl}?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}&grant_type=client_credentials";
        using var response = await _http.PostAsync(tokenUrl, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("IGDB token request failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<IgdbTokenResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            return null;
        }

        _accessToken = token.AccessToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn, 60));
        return _accessToken;
    }

    private static string CreateGameQuery(Game game)
    {
        var igdbId = game.ExternalIds.GetExternalId(ExternalMediaProvider.Igdb);
        if (!string.IsNullOrWhiteSpace(igdbId) && int.TryParse(igdbId, out var numericId))
        {
            return $"fields name,first_release_date,cover.url; where id = {numericId}; limit 1;";
        }

        return $"search \"{Escape(game.Name)}\"; fields name,first_release_date,cover.url; limit 1;";
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string? NormalizeCoverUrl(string? coverUrl)
    {
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            return null;
        }

        var normalized = coverUrl.StartsWith("//", StringComparison.Ordinal) ? $"https:{coverUrl}" : coverUrl;
        return normalized.Replace("/t_thumb/", "/t_cover_big/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class IgdbTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class IgdbGame
    {
        [JsonPropertyName("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonPropertyName("cover")]
        public IgdbCover? Cover { get; set; }
    }

    private sealed class IgdbCover
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
