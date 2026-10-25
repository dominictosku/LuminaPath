using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class GoogleDriveConnectionService : IDisposable
{
    public const string Scope = "https://www.googleapis.com/auth/drive.file";
    private readonly HttpClient _http;
    private readonly GoogleDriveOptions _options;
    private readonly GoogleDriveConnectionStore _store;
    private readonly MemoryCache _pending = new(new MemoryCacheOptions { SizeLimit = 32 });
    private readonly object _stateGate = new();

    public GoogleDriveConnectionService(HttpClient http, IOptions<GoogleDriveOptions> options, GoogleDriveConnectionStore store)
    {
        _http = http;
        _options = options.Value;
        _store = store;
    }

    public string BuildConnectUrl(string userId)
    {
        if (!_options.CanConnectInBrowser || _options.HasExternalToken || string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Browser authorization is unavailable. Check the Drive configuration.");
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _pending.Set(state, new PendingConnection(userId, verifier), new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), Size = 1
        });
        return QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId, ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code", ["scope"] = Scope, ["access_type"] = "offline",
            ["prompt"] = "consent", ["state"] = state, ["code_challenge_method"] = "S256",
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
        });
    }

    public async Task CompleteAsync(string userId, string? state, string? code, string? error, CancellationToken ct)
    {
        PendingConnection? pending;
        lock (_stateGate)
        {
            pending = state is null ? null : _pending.Get<PendingConnection>(state);
            if (state is not null) _pending.Remove(state); // Single-use even on denial or failure.
        }
        if (pending is null || pending.UserId != userId || string.IsNullOrWhiteSpace(code)
            || !string.IsNullOrEmpty(error) || _options.HasExternalToken)
            throw new InvalidOperationException("Google authorization was denied, invalid or expired. Start again.");

        using var response = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId, ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri!, ["code"] = code,
                ["code_verifier"] = pending.Verifier, ["grant_type"] = "authorization_code"
            }), ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Google authorization failed. Start again.");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var token = json.RootElement;
        if (!token.TryGetProperty("refresh_token", out var refresh) || string.IsNullOrWhiteSpace(refresh.GetString())
            || !token.TryGetProperty("scope", out var scopes) || !(scopes.GetString() ?? "").Split(' ').Contains(Scope)
            || !token.TryGetProperty("access_token", out var access) || string.IsNullOrWhiteSpace(access.GetString()))
            throw new InvalidOperationException("Google did not grant offline Drive access. Start again and allow access.");

        using var about = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/drive/v3/about?fields=user(permissionId)");
        about.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.GetString());
        using var identityResponse = await _http.SendAsync(about, ct);
        if (!identityResponse.IsSuccessStatusCode) throw new InvalidOperationException("Cannot verify the Google Drive account.");
        using var identity = await JsonDocument.ParseAsync(await identityResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var accountId = identity.RootElement.GetProperty("user").GetProperty("permissionId").GetString();
        if (string.IsNullOrWhiteSpace(accountId)) throw new InvalidOperationException("Cannot verify the Google Drive account.");
        _store.Save(refresh.GetString()!, accountId);
    }

    public void Dispose() { _http.Dispose(); _pending.Dispose(); }
    private sealed record PendingConnection(string UserId, string Verifier);
}
