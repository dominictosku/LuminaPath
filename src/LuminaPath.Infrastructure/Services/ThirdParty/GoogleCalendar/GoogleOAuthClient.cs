using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Google OAuth 2.0 calls (authorization URL, code exchange, refresh, revoke).
/// Raw HTTP to stay consistent with the other third-party clients and avoid a
/// heavy SDK dependency.
/// </summary>
public interface IGoogleOAuthClient
{
    Task<string> BuildAuthorizationUrlAsync(string redirectUri, string state, CancellationToken cancellationToken);
    Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeAsync(string token, CancellationToken cancellationToken);
}

public sealed class GoogleOAuthClient : IGoogleOAuthClient
{
    private readonly HttpClient _http;
    private readonly GoogleCalendarSettingsResolver _resolver;

    public GoogleOAuthClient(HttpClient http, GoogleCalendarSettingsResolver resolver)
    {
        _http = http;
        _resolver = resolver;
    }

    public async Task<string> BuildAuthorizationUrlAsync(string redirectUri, string state, CancellationToken cancellationToken)
    {
        var settings = await _resolver.GetAsync(cancellationToken);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = settings.Scope,
            ["access_type"] = "offline",
            ["include_granted_scopes"] = "true",
            // Force a refresh token even on re-consent.
            ["prompt"] = "consent",
            ["state"] = state,
        };

        var encoded = string.Join('&', query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        return $"{settings.AuthorizationEndpoint}?{encoded}";
    }

    public async Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var settings = await _resolver.GetAsync(cancellationToken);
        using var response = await PostFormAsync(settings.TokenEndpoint, new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId ?? string.Empty,
            ["client_secret"] = settings.ClientSecret ?? string.Empty,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        using var doc = await ReadJsonAsync(response, cancellationToken);
        var root = doc.RootElement;

        return new GoogleTokenResult(
            AccessToken: root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken: GetString(root, "refresh_token"),
            Email: ExtractEmailFromIdToken(GetString(root, "id_token")));
    }

    public async Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var settings = await _resolver.GetAsync(cancellationToken);
        using var response = await PostFormAsync(settings.TokenEndpoint, new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId ?? string.Empty,
            ["client_secret"] = settings.ClientSecret ?? string.Empty,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        using var doc = await ReadJsonAsync(response, cancellationToken);
        var root = doc.RootElement;

        // Google does not reissue a refresh token on refresh.
        return new GoogleTokenResult(
            AccessToken: root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken: null,
            Email: null);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var settings = await _resolver.GetAsync(cancellationToken);
        using var response = await PostFormAsync(settings.RevokeEndpoint, new Dictionary<string, string>
        {
            ["token"] = token,
        }, cancellationToken);
        // Best-effort: a revoked/expired token returns 400; ignore.
    }

    private Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return _http.SendAsync(request, cancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Pull the email claim out of the id_token JWT payload. The token came
    /// straight from Google over TLS, so the unsigned payload is trustworthy
    /// for display purposes (we don't authenticate with it).
    /// </summary>
    private static string? ExtractEmailFromIdToken(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payload);
            return GetString(doc.RootElement, "email");
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
