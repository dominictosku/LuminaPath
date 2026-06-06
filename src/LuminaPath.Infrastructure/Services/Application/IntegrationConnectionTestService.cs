using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.ThirdParty;
using LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class IntegrationConnectionTestService
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string SteamTestUserId = "76561197960435530";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamOptions _steamOptions;
    private readonly PsnOptions _psnOptions;
    private readonly GameMetadataOptions _metadataOptions;
    private readonly GoogleCalendarOptions _googleOptions;

    public IntegrationConnectionTestService(
        IHttpClientFactory httpClientFactory,
        IOptions<SteamOptions> steamOptions,
        IOptions<PsnOptions> psnOptions,
        IOptions<GameMetadataOptions> metadataOptions,
        IOptions<GoogleCalendarOptions> googleOptions)
    {
        _httpClientFactory = httpClientFactory;
        _steamOptions = steamOptions.Value;
        _psnOptions = psnOptions.Value;
        _metadataOptions = metadataOptions.Value;
        _googleOptions = googleOptions.Value;
    }

    public async Task<IntegrationConnectionTestResult> TestAiAsync(
        AiChatStoredSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(settings.Enabled, out var enabled) || !enabled)
        {
            return IntegrationConnectionTestResult.Failed("AI", "AI chat is disabled in the current settings.");
        }

        return NormalizeProvider(settings.Provider) switch
        {
            "openai" or "ollama" => await TestOpenAiCompatibleAsync(settings, cancellationToken),
            _ => await TestAnthropicAsync(settings, cancellationToken)
        };
    }

    public async Task<IntegrationConnectionTestResult> TestSteamAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return IntegrationConnectionTestResult.Failed("Steam", "Steam API key is missing.");
        }

        var url = $"{_steamOptions.ApiBaseUrl.TrimEnd('/')}/ISteamUser/GetPlayerSummaries/v2/"
            + $"?key={Uri.EscapeDataString(apiKey.Trim())}&steamids={SteamTestUserId}";
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return IntegrationConnectionTestResult.Failed(
                "Steam",
                $"Steam returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
        }

        using var doc = await ReadJsonAsync(response, cancellationToken);
        var players = doc.RootElement
            .GetProperty("response")
            .GetProperty("players");
        return players.ValueKind == JsonValueKind.Array && players.GetArrayLength() > 0
            ? IntegrationConnectionTestResult.Success("Steam", "Steam API key works.")
            : IntegrationConnectionTestResult.Failed("Steam", "Steam responded, but the expected profile payload was empty.");
    }

    public async Task<IntegrationConnectionTestResult> TestPsnAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return IntegrationConnectionTestResult.Failed("PSN", "PSN bearer token is missing.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_psnOptions.ProfileBaseUrl.TrimEnd('/')}/userProfile/v1/users/me/profile2?fields=accountId,onlineId,currentOnlineId");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
        using var response = await SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return IntegrationConnectionTestResult.Failed(
                "PSN",
                $"PSN returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
        }

        using var doc = await ReadJsonAsync(response, cancellationToken);
        var profile = doc.RootElement.TryGetProperty("profile", out var value) ? value : default;
        var onlineId = GetString(profile, "onlineId");
        var accountId = GetString(profile, "accountId");

        return string.IsNullOrWhiteSpace(onlineId) && string.IsNullOrWhiteSpace(accountId)
            ? IntegrationConnectionTestResult.Failed("PSN", "PSN responded, but the profile payload was empty.")
            : IntegrationConnectionTestResult.Success("PSN", "PSN bearer token works.", onlineId ?? accountId);
    }

    public async Task<IntegrationConnectionTestResult> TestIgdbAsync(
        GameMetadataSettings settings,
        CancellationToken cancellationToken = default)
    {
        var clientId = FirstNonEmpty(settings.IgdbClientId, _metadataOptions.IgdbClientId);
        var clientSecret = FirstNonEmpty(settings.IgdbClientSecret, _metadataOptions.IgdbClientSecret);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return IntegrationConnectionTestResult.Failed("IGDB", "IGDB client ID and secret are required.");
        }

        var tokenUrl = $"{_metadataOptions.TwitchTokenUrl}?client_id={Uri.EscapeDataString(clientId)}"
            + $"&client_secret={Uri.EscapeDataString(clientSecret)}&grant_type=client_credentials";
        using var tokenResponse = await SendAsync(new HttpRequestMessage(HttpMethod.Post, tokenUrl), cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return IntegrationConnectionTestResult.Failed(
                "IGDB",
                $"Twitch OAuth returned {(int)tokenResponse.StatusCode} {tokenResponse.StatusCode}.",
                await ReadBodySnippetAsync(tokenResponse, cancellationToken));
        }

        using var tokenDoc = await ReadJsonAsync(tokenResponse, cancellationToken);
        var token = GetString(tokenDoc.RootElement, "access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return IntegrationConnectionTestResult.Failed("IGDB", "Twitch OAuth did not return an access token.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_metadataOptions.IgdbBaseUrl.TrimEnd('/')}/games")
        {
            Content = new StringContent("search \"Portal\"; fields name; limit 1;", Encoding.UTF8, "text/plain")
        };
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode
            ? IntegrationConnectionTestResult.Success("IGDB", "IGDB token and games endpoint work.")
            : IntegrationConnectionTestResult.Failed(
                "IGDB",
                $"IGDB returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
    }

    public async Task<IntegrationConnectionTestResult> TestRawgAsync(
        GameMetadataSettings settings,
        CancellationToken cancellationToken = default)
    {
        var apiKey = FirstNonEmpty(settings.RawgApiKey, _metadataOptions.RawgApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return IntegrationConnectionTestResult.Failed("RAWG", "RAWG API key is required.");
        }

        var url = $"{_metadataOptions.RawgBaseUrl.TrimEnd('/')}/games"
            + $"?key={Uri.EscapeDataString(apiKey)}&search=Portal&page_size=1";
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

        return response.IsSuccessStatusCode
            ? IntegrationConnectionTestResult.Success("RAWG", "RAWG API key and games endpoint work.")
            : IntegrationConnectionTestResult.Failed(
                "RAWG",
                $"RAWG returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
    }

    public async Task<IntegrationConnectionTestResult> TestGoogleCalendarAsync(
        GoogleCalendarSettings settings,
        CancellationToken cancellationToken = default)
    {
        var clientId = FirstNonEmpty(settings.ClientId, _googleOptions.ClientId);
        var clientSecret = FirstNonEmpty(settings.ClientSecret, _googleOptions.ClientSecret);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return IntegrationConnectionTestResult.Failed("Google Calendar", "Google OAuth client ID and secret are required.");
        }

        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"{_googleOptions.CalendarApiBaseUrl.TrimEnd('/')}/users/me/calendarList"),
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return IntegrationConnectionTestResult.Success(
                "Google Calendar",
                "Google Calendar API is reachable.",
                "OAuth credentials are present. User consent still validates the redirect URI and account access.");
        }

        return response.IsSuccessStatusCode
            ? IntegrationConnectionTestResult.Success("Google Calendar", "Google Calendar API is reachable.")
            : IntegrationConnectionTestResult.Failed(
                "Google Calendar",
                $"Google Calendar returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
    }

    private async Task<IntegrationConnectionTestResult> TestAnthropicAsync(
        AiChatStoredSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AnthropicApiKey))
        {
            return IntegrationConnectionTestResult.Failed("Anthropic", "Anthropic API key is missing.");
        }

        var body = new
        {
            model = FirstNonEmpty(settings.AnthropicModel, "claude-sonnet-4-6"),
            max_tokens = Math.Clamp(ParseInt(settings.AnthropicMaxTokens, 32), 1, 128),
            messages = new[] { new { role = "user", content = "Reply with OK." } },
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{FirstNonEmpty(settings.AnthropicBaseUrl, "https://api.anthropic.com").TrimEnd('/')}/v1/messages")
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
        request.Headers.Add("x-api-key", settings.AnthropicApiKey.Trim());
        request.Headers.Add("anthropic-version", FirstNonEmpty(settings.AnthropicVersion, "2023-06-01"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? IntegrationConnectionTestResult.Success("Anthropic", "Anthropic responded successfully.", body.model)
            : IntegrationConnectionTestResult.Failed(
                "Anthropic",
                $"Anthropic returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
    }

    private async Task<IntegrationConnectionTestResult> TestOpenAiCompatibleAsync(
        AiChatStoredSettings settings,
        CancellationToken cancellationToken)
    {
        var baseUrl = FirstNonEmpty(settings.OpenAiBaseUrl, "http://localhost:11434/v1");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return IntegrationConnectionTestResult.Failed("OpenAI-compatible", "Base URL is missing.");
        }

        var body = new
        {
            model = FirstNonEmpty(settings.OpenAiModel, "llama3.2"),
            messages = new[] { new { role = "user", content = "Reply with OK." } },
            max_tokens = Math.Clamp(ParseInt(settings.OpenAiMaxTokens, 32), 1, 128),
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
        if (!string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey.Trim());
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? IntegrationConnectionTestResult.Success("OpenAI-compatible", "OpenAI-compatible endpoint responded successfully.", body.model)
            : IntegrationConnectionTestResult.Failed(
                "OpenAI-compatible",
                $"Endpoint returned {(int)response.StatusCode} {response.StatusCode}.",
                await ReadBodySnippetAsync(response, cancellationToken));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TestTimeout);
        var client = _httpClientFactory.CreateClient();
        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent($"Connection test timed out after {TestTimeout.TotalSeconds:0} seconds.")
            };
        }
        catch (Exception ex)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(ex.Message)
            };
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task<string?> ReadBodySnippetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        body = body.Trim();
        return body.Length <= 280 ? body : body[..280] + "...";
    }

    private static string? GetString(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    private static int ParseInt(string value, int fallback)
        => int.TryParse(value, out var parsed) ? parsed : fallback;

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string NormalizeProvider(string provider)
        => string.IsNullOrWhiteSpace(provider)
            ? "anthropic"
            : provider.Trim().ToLowerInvariant();
}

public sealed record IntegrationConnectionTestResult(
    string Provider,
    bool Succeeded,
    string Message,
    string? Details)
{
    public static IntegrationConnectionTestResult Success(string provider, string message, string? details = null)
        => new(provider, true, message, details);

    public static IntegrationConnectionTestResult Failed(string provider, string message, string? details = null)
        => new(provider, false, message, details);
}
