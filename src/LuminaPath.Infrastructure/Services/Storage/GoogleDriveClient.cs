using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Private My Drive storage. No sharing permissions or public URLs are created.</summary>
public sealed class GoogleDriveClient : IDisposable
{
    private const string Api = "https://www.googleapis.com/drive/v3/";
    private const string FolderType = "application/vnd.google-apps.folder";
    private readonly HttpClient _http;
    private readonly GoogleDriveOptions _options;
    private readonly GoogleDriveConnectionStore? _connection;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly SemaphoreSlim _folderLock = new(1, 1);
    private readonly Dictionary<string, string> _folders = new();
    private string? _accessToken;
    private DateTimeOffset _expires;

    public GoogleDriveClient(HttpClient http, IOptions<GoogleDriveOptions> options, GoogleDriveConnectionStore? connection = null)
    {
        _http = http;
        _options = options.Value;
        _connection = connection;
    }

    public async Task<string> GetFolderAsync(string area, CancellationToken cancellationToken = default)
    {
        if (area is not ("uploads" or "backups")) throw new ArgumentException("Unknown storage area.", nameof(area));
        await _folderLock.WaitAsync(cancellationToken);
        try
        {
            if (_folders.TryGetValue(area, out var cached)) return cached;
            if (!_folders.TryGetValue("root", out var root))
            {
                if (!string.IsNullOrWhiteSpace(_options.FolderId))
                {
                    using var response = await SendAsync(() => new(HttpMethod.Get,
                        Api + "files/" + Uri.EscapeDataString(_options.FolderId) + "?fields=id,mimeType,trashed"), cancellationToken);
                    await EnsureSuccessAsync(response);
                    var folder = await response.Content.ReadFromJsonAsync<DriveFile>(cancellationToken);
                    if (folder is null || folder.Trashed || folder.MimeType != FolderType)
                        throw new InvalidOperationException("GoogleDrive:FolderId must identify an accessible, non-trashed folder.");
                    root = folder.Id;
                }
                else
                {
                    root = await FindOrCreateFolderAsync("root", _options.FolderName,
                        "appProperties has { key='luminaPathRoot' and value='v1' }", cancellationToken);
                }
                _folders["root"] = root;
            }
            var id = await FindOrCreateFolderAsync(root, area,
                "appProperties has { key='luminaPathArea' and value='" + area + "' }", cancellationToken);
            _folders[area] = id;
            return id;
        }
        finally { _folderLock.Release(); }
    }

    private async Task<string> FindOrCreateFolderAsync(string parent, string name, string marker, CancellationToken ct)
    {
        var matches = await QueryAsync($"'{Escape(parent)}' in parents and name='{Escape(name)}' and mimeType='{FolderType}' and {marker}", ct);
        if (matches.Count > 1) throw new InvalidOperationException("Multiple LuminaPath folders found. Select a root using GoogleDrive:FolderId and remove duplicate area folders.");
        if (matches.Count == 1) return matches[0].Id;
        var properties = parent == "root"
            ? new Dictionary<string, string> { ["luminaPathRoot"] = "v1" }
            : new Dictionary<string, string> { ["luminaPathArea"] = name };
        // Preallocated IDs make retrying a create safe: Drive rejects a duplicate ID.
        var id = await GenerateIdAsync(ct);
        using var response = await SendAsync(() => new(HttpMethod.Post, Api + "files?fields=id")
        {
            Content = JsonContent.Create(new { id, name, mimeType = FolderType, parents = new[] { parent }, appProperties = properties })
        }, ct);
        if (response.StatusCode != HttpStatusCode.Conflict) await EnsureSuccessAsync(response);
        return id;
    }

    public async Task<List<DriveFile>> ListAsync(string area, CancellationToken ct = default)
    {
        var folder = await GetFolderAsync(area, ct);
        return await QueryAsync($"'{Escape(folder)}' in parents and mimeType!='{FolderType}'", ct);
    }

    public async Task<DriveFile?> FindAsync(string area, string name, CancellationToken ct = default)
    {
        var folder = await GetFolderAsync(area, ct);
        var files = await QueryAsync($"'{Escape(folder)}' in parents and name='{Escape(name)}' and mimeType!='{FolderType}'", ct);
        if (files.Count > 1) throw new InvalidOperationException("Duplicate filenames in the LuminaPath Drive folder. Resolve them before continuing.");
        return files.SingleOrDefault();
    }

    private async Task<List<DriveFile>> QueryAsync(string query, CancellationToken ct)
    {
        var files = new List<DriveFile>();
        string? page = null;
        do
        {
            var url = Api + "files?spaces=drive&pageSize=1000&fields=nextPageToken,files(id,name,mimeType,size,modifiedTime,trashed)&q="
                + Uri.EscapeDataString("trashed=false and " + query)
                + (page is null ? "" : "&pageToken=" + Uri.EscapeDataString(page));
            using var response = await SendAsync(() => new(HttpMethod.Get, url), ct);
            await EnsureSuccessAsync(response);
            var result = await response.Content.ReadFromJsonAsync<DriveList>(ct)
                ?? throw new InvalidOperationException("Google Drive returned an empty file listing.");
            files.AddRange(result.Files);
            page = result.NextPageToken;
        } while (!string.IsNullOrEmpty(page));
        return files;
    }

    private async Task<string> GenerateIdAsync(CancellationToken ct)
    {
        using var response = await SendAsync(() => new(HttpMethod.Get, Api + "files/generateIds?count=1&space=drive&type=files"), ct);
        await EnsureSuccessAsync(response);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return json.RootElement.GetProperty("ids")[0].GetString()!;
    }

    public async Task UploadAsync(string area, string name, Stream source, string contentType, CancellationToken ct = default)
    {
        if (!source.CanSeek) throw new ArgumentException("Drive uploads require a seekable stream.", nameof(source));
        var folder = await GetFolderAsync(area, ct);
        if (await FindAsync(area, name, ct) is not null) throw new IOException("A file with this name already exists in Google Drive.");
        var id = await GenerateIdAsync(ct);
        var start = source.Position;
        var length = source.Length - start;
        if (length == 0)
        {
            using var empty = await SendAsync(() => new(HttpMethod.Post, Api + "files?fields=id")
            {
                Content = JsonContent.Create(new { id, name, mimeType = contentType, parents = new[] { folder } })
            }, ct);
            if (empty.StatusCode != HttpStatusCode.Conflict) await EnsureSuccessAsync(empty);
            return;
        }
        using var init = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable&fields=id");
            request.Content = JsonContent.Create(new { id, name, mimeType = contentType, parents = new[] { folder } });
            request.Headers.TryAddWithoutValidation("X-Upload-Content-Type", contentType);
            request.Headers.TryAddWithoutValidation("X-Upload-Content-Length", length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return request;
        }, ct);
        await EnsureSuccessAsync(init);
        var session = init.Headers.Location;
        if (session is null || !session.IsAbsoluteUri || session.Scheme != "https" || session.Host != "www.googleapis.com")
            throw new InvalidOperationException("Google Drive returned an invalid upload session.");

        var buffer = new byte[1024 * 1024]; // Multiple of Drive's 256 KiB chunk size.
        long offset = 0;
        var failures = 0;
        while (offset < length)
        {
            source.Position = start + offset;
            var count = (int)Math.Min(buffer.Length, length - offset);
            await source.ReadExactlyAsync(buffer.AsMemory(0, count), ct);
            HttpResponseMessage? response = null;
            try
            {
                response = await SendAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, session);
                    request.Content = new ByteArrayContent(buffer, 0, count);
                    request.Content.Headers.ContentType = new(contentType);
                    request.Content.Headers.ContentRange = new(offset, offset + count - 1, length);
                    return request;
                }, ct, retry: false);
                if (response.IsSuccessStatusCode) return;
                if ((int)response.StatusCode == 308)
                {
                    var next = NextOffset(response, length);
                    if (next > offset) { offset = next; failures = 0; continue; }
                }
                else if (!await IsTransientAsync(response)) await EnsureSuccessAsync(response);
            }
            catch (HttpRequestException) { /* Probe the session before resending bytes. */ }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
            finally { response?.Dispose(); }

            if (++failures > 4) throw new IOException("Google Drive upload could not make progress. Retry the operation.");
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(8, 1 << failures)), ct);
            using var status = await SendAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, session) { Content = new ByteArrayContent([]) };
                request.Content.Headers.ContentRange = new(length);
                return request;
            }, ct);
            if (status.IsSuccessStatusCode) return;
            if ((int)status.StatusCode != 308) await EnsureSuccessAsync(status);
            offset = NextOffset(status, length);
        }
        throw new IOException("Google Drive did not confirm completion of the upload.");
    }

    private static long NextOffset(HttpResponseMessage response, long length)
    {
        if (!response.Headers.TryGetValues("Range", out var ranges)) return 0;
        var range = ranges.Single();
        if (!range.StartsWith("bytes=0-", StringComparison.Ordinal)
            || !long.TryParse(range[8..], out var end) || end < 0 || end >= length)
            throw new IOException("Google Drive returned an invalid upload offset.");
        return end + 1;
    }

    public async Task DownloadAsync(string id, Stream destination, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new(HttpMethod.Get, Api + "files/" + Uri.EscapeDataString(id) + "?alt=media"), ct);
        await EnsureSuccessAsync(response);
        await response.Content.CopyToAsync(destination, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new(HttpMethod.Delete, Api + "files/" + Uri.EscapeDataString(id)), ct);
        if (response.StatusCode != HttpStatusCode.NotFound) await EnsureSuccessAsync(response);
    }

    public async Task RenameAsync(string id, string name, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new(HttpMethod.Patch, Api + "files/" + Uri.EscapeDataString(id))
        { Content = JsonContent.Create(new { name }) }, ct);
        await EnsureSuccessAsync(response);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expires) return _accessToken;
            var refreshToken = _options.HasExternalToken ? _options.RefreshToken : _connection?.Read()?.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("Connect Google Drive from /Admin/Overview?tab=storage first.");
            using var response = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId, ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = refreshToken, ["grant_type"] = "refresh_token"
            }), ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Google Drive authorization failed. Check credentials or authorize the account again.");
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            _accessToken = json.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("Google did not return an access token.");
            _expires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, json.RootElement.GetProperty("expires_in").GetInt32() - 60));
            return _accessToken;
        }
        finally { _tokenLock.Release(); }
    }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> create, CancellationToken ct, bool retry = true)
    {
        var refreshed = false;
        for (var attempt = 0; ; attempt++)
        {
            using var request = create();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
            HttpResponseMessage response;
            try { response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); }
            catch (HttpRequestException) when (retry && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct);
                continue;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized && !refreshed)
            {
                response.Dispose();
                await _tokenLock.WaitAsync(ct);
                try { _accessToken = null; } finally { _tokenLock.Release(); }
                refreshed = true;
                continue;
            }
            if (!retry || attempt >= 3 || !await IsTransientAsync(response)) return response;
            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1 << attempt);
            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(delay.TotalSeconds, 0, 30)), ct);
        }
    }

    private static async Task<bool> IsTransientAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) return true;
        if (response.StatusCode != HttpStatusCode.Forbidden) return false;
        var body = await response.Content.ReadAsStringAsync();
        return body.Contains("userRateLimitExceeded", StringComparison.Ordinal) || body.Contains("rateLimitExceeded", StringComparison.Ordinal);
    }

    private static Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            // Never propagate Google's body, URL, OAuth tokens or upload-session URL to logs/users.
            throw new InvalidOperationException($"Google Drive request failed (HTTP {(int)response.StatusCode}). Check authorization, quota and connectivity.");
        return Task.CompletedTask;
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
    public void Dispose() { _http.Dispose(); _tokenLock.Dispose(); _folderLock.Dispose(); }
    private sealed record DriveList(List<DriveFile> Files, string? NextPageToken);
}

public sealed record DriveFile(string Id, string Name, string MimeType,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] long Size = 0,
    DateTimeOffset? ModifiedTime = null,
    bool Trashed = false);
