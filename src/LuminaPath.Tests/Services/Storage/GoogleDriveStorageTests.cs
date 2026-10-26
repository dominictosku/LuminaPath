using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LuminaPath.Tests.Services.Storage;

public sealed class GoogleDriveStorageTests
{
    [Fact]
    public async Task UploadDownloadRenameDelete_PreserveNamesAndPrivateUrls()
    {
        using var fixture = new DriveFixture();
        var upload = await fixture.Storage.UploadAsync(new MemoryStream([1, 2, 3]), "cover", "image/png");
        Assert.False(upload.Error);
        Assert.Equal("cover.png", upload.Blob.Name);
        Assert.Equal("api/files/cover.png", upload.Blob.Uri);
        var first = await fixture.Storage.DownloadAsync("cover.png");
        await using (var stream = first!.Content!) Assert.Equal(new byte[] { 1, 2, 3 }, ReadAll(stream));
        var cached = await fixture.Storage.DownloadAsync("cover.png");
        cached!.Content!.Dispose();
        Assert.Equal(1, fixture.Handler.Downloads);
        Assert.True(await fixture.Storage.RenameAsync("cover.png", "new-name"));
        Assert.Null(await fixture.Storage.DownloadAsync("cover.png"));
        Assert.Equal("new-name.png", Assert.Single(await fixture.Storage.ListAsync()).Name);
        Assert.False((await fixture.Storage.DeleteAsync("new-name.png")).Error);
        Assert.Empty(await fixture.Storage.ListAsync());
        Assert.Equal(1, fixture.Handler.TokenRequests);
    }

    [Fact]
    public async Task ResumableUpload_SplitsLargeFilesWithoutChangingContent()
    {
        using var fixture = new DriveFixture();
        var bytes = new byte[2 * 1024 * 1024 + 19];
        new Random(42).NextBytes(bytes);
        var result = await fixture.Storage.UploadAsync(new MemoryStream(bytes), "large.bin");
        Assert.False(result.Error);
        Assert.Equal(3, fixture.Handler.Chunks);
        Assert.Equal(bytes, Assert.Single(fixture.Handler.Files.Values).Bytes);
    }

    [Fact]
    public async Task InterruptedChunk_QueriesAcceptedOffsetBeforeResuming()
    {
        using var fixture = new DriveFixture();
        fixture.Handler.DisconnectAfterChunk = true;
        var bytes = new byte[1024 * 1024 + 3];
        new Random(11).NextBytes(bytes);
        var result = await fixture.Storage.UploadAsync(new MemoryStream(bytes), "resumed.bin");
        Assert.False(result.Error);
        Assert.Equal(1, fixture.Handler.StatusProbes);
        Assert.Equal(bytes, Assert.Single(fixture.Handler.Files.Values).Bytes);
    }

    [Fact]
    public async Task BackupsAreSeparateFromUploadListingAndDeletion()
    {
        using var fixture = new DriveFixture();
        await fixture.Client.UploadAsync("backups", "db.dump", new MemoryStream([42]), "application/octet-stream");
        Assert.Empty(await fixture.Storage.ListAsync());
        Assert.True((await fixture.Storage.DeleteAsync("db.dump")).Error);
        Assert.Equal("backups-folder", Assert.Single(fixture.Handler.Files.Values).Parent);
    }

    [Fact]
    public async Task DuplicateNamesAreNotOverwrittenOrArbitrarilyDeleted()
    {
        using var fixture = new DriveFixture();
        fixture.Handler.Files["one"] = new("cover.png", "uploads-folder", "image/png", [1]);
        var duplicate = await fixture.Storage.UploadAsync(new MemoryStream([2]), "cover.png");
        Assert.True(duplicate.Error);
        fixture.Handler.Files["two"] = new("cover.png", "uploads-folder", "image/png", [2]);
        Assert.True((await fixture.Storage.DeleteAsync("cover.png")).Error);
        Assert.Equal(2, fixture.Handler.Files.Count);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    public async Task ExpiredTokensAndThrottlingAreRetried(int status)
    {
        using var fixture = new DriveFixture();
        fixture.Handler.NextStatus = status;
        Assert.Empty(await fixture.Storage.ListAsync());
        Assert.Equal(status == 401 ? 2 : 1, fixture.Handler.TokenRequests);
    }

    [Fact]
    public async Task RevokedAuthorizationDoesNotExposeSecretResponseBody()
    {
        using var fixture = new DriveFixture();
        fixture.Handler.RejectToken = true;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Storage.ListAsync());
        Assert.DoesNotContain("private-token", error.ToString());
        Assert.Contains("authorize", error.Message);
    }

    [Fact]
    public async Task ListingsFollowPagination()
    {
        using var fixture = new DriveFixture();
        fixture.Handler.Paginate = true;
        fixture.Handler.Files["one"] = new("one.png", "uploads-folder", "image/png", [1]);
        fixture.Handler.Files["two"] = new("two.png", "uploads-folder", "image/png", [2]);
        Assert.Equal(2, (await fixture.Storage.ListAsync()).Count);
    }

    [Fact]
    public async Task NewAccountCreatesDedicatedRootAndUploadFolder()
    {
        using var fixture = new DriveFixture(folderId: null);
        Assert.Empty(await fixture.Storage.ListAsync());
        Assert.Equal(new[] { "LuminaPath", "uploads" }, fixture.Handler.CreatedFolders);
    }

    [Fact]
    public async Task MigrationRetainsSourceAndVerifiesExistingFiles()
    {
        using var fixture = new DriveFixture();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory, "cover.png"), [1, 2]);
            Directory.CreateDirectory(Path.Combine(directory, "backups"));
            await File.WriteAllBytesAsync(Path.Combine(directory, "backups", "db.dump"), [3]);
            var migration = new StorageMigrationService(NullLogger<StorageMigrationService>.Instance);
            var source = new FileSystemStorage(directory, NullLogger<FileSystemStorage>.Instance);
            Assert.Equal(1, (await migration.MigrateAsync(source, fixture.Storage, dryRun: true)).Copied);
            Assert.Empty(fixture.Handler.Files);
            Assert.Equal(1, (await migration.MigrateAsync(source, fixture.Storage, dryRun: false)).Copied);
            Assert.True(File.Exists(Path.Combine(directory, "cover.png")));
            Assert.Equal(1, (await migration.MigrateAsync(source, fixture.Storage, dryRun: false)).Skipped);
            await File.WriteAllBytesAsync(Path.Combine(directory, "cover.png"), [9]);
            await Assert.ThrowsAsync<IOException>(() => migration.MigrateAsync(source, fixture.Storage, dryRun: false));
            Assert.Equal(new byte[] { 1, 2 }, Assert.Single(fixture.Handler.Files.Values).Bytes);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task MigrationFromDrive_PreservesEmptyAndExtensionlessFiles()
    {
        using var fixture = new DriveFixture();
        Assert.False((await fixture.Storage.UploadExactAsync(new MemoryStream([]), "empty", "image/png")).Error);
        Assert.False((await fixture.Storage.UploadExactAsync(new MemoryStream([1, 2]), "cover", "image/png")).Error);
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var filesystem = new FileSystemStorage(directory, NullLogger<FileSystemStorage>.Instance);
            var migration = new StorageMigrationService(NullLogger<StorageMigrationService>.Instance);
            Assert.Equal(2, (await migration.MigrateAsync(fixture.Storage, filesystem, false)).Copied);
            Assert.Empty(await File.ReadAllBytesAsync(Path.Combine(directory, "empty")));
            Assert.Equal(new byte[] { 1, 2 }, await File.ReadAllBytesAsync(Path.Combine(directory, "cover")));
            Assert.Equal(2, (await migration.MigrateAsync(filesystem, fixture.Storage, false)).Skipped);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private sealed class DriveFixture : IDisposable
    {
        public FakeDrive Handler { get; } = new();
        public GoogleDriveClient Client { get; }
        public GoogleDriveStorage Storage { get; }
        public DriveFixture(string? folderId = "root-folder")
        {
            var options = Options.Create(new GoogleDriveOptions
            {
                ClientId = "test-client", ClientSecret = "private-token", RefreshToken = "private-token", FolderId = folderId
            });
            Handler.NewAccount = folderId is null;
            Client = new(new HttpClient(Handler), options);
            Storage = new(Client, options, NullLogger<GoogleDriveStorage>.Instance);
        }
        public void Dispose() { Storage.Dispose(); Client.Dispose(); }
    }

    private sealed record StoredFile(string Name, string Parent, string Type, byte[] Bytes);
    private sealed class FakeDrive : HttpMessageHandler
    {
        public Dictionary<string, StoredFile> Files { get; } = new();
        public List<string> CreatedFolders { get; } = [];
        public int Downloads { get; private set; }
        public int Chunks { get; private set; }
        public int TokenRequests { get; private set; }
        public bool DisconnectAfterChunk { get; set; }
        public int StatusProbes { get; private set; }
        public int NextStatus { get; set; }
        public bool RejectToken { get; set; }
        public bool Paginate { get; set; }
        public bool NewAccount { get; set; }
        private int _nextId;
        private string _uploadId = "";
        private StoredFile? _upload;
        private readonly MemoryStream _bytes = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri!;
            if (uri.Host == "oauth2.googleapis.com")
            {
                TokenRequests++;
                return RejectToken ? Json(new { error = "private-token" }, 400) : Json(new { access_token = "access-token", expires_in = 3600 });
            }
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            if (NextStatus != 0)
            {
                var status = NextStatus;
                NextStatus = 0;
                var response = Json(new { error = "retry" }, status);
                response.Headers.RetryAfter = new(TimeSpan.Zero);
                return response;
            }
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (uri.AbsolutePath.EndsWith("generateIds")) return Json(new { ids = new[] { $"id-{++_nextId}" } });
            if (uri.AbsolutePath == "/drive/v3/files/root-folder")
                return Json(new { id = "root-folder", name = "LuminaPath", mimeType = "application/vnd.google-apps.folder" });
            if (uri.AbsolutePath == "/drive/v3/files" && request.Method == HttpMethod.Get)
            {
                var q = query["q"].ToString();
                Assert.Contains("trashed=false", q);
                if (q.Contains("appProperties"))
                {
                    if (NewAccount) return Json(new { files = Array.Empty<object>() });
                    var area = q.Contains("value='backups'") ? "backups" : "uploads";
                    return Json(new { files = new[] { new { id = area + "-folder", name = area, mimeType = "application/vnd.google-apps.folder" } } });
                }
                var parent = Regex.Match(q, "'([^']+)' in parents").Groups[1].Value;
                var name = Regex.Match(q, "name='([^']+)'");
                var files = Files.Where(pair => pair.Value.Parent == parent && (!name.Success || pair.Value.Name == name.Groups[1].Value))
                    .Select(pair => new { id = pair.Key, name = pair.Value.Name, mimeType = pair.Value.Type, size = pair.Value.Bytes.Length.ToString() }).ToArray();
                if (Paginate && files.Length > 1)
                    return query.ContainsKey("pageToken") ? Json(new { files = files.Skip(1).ToArray() })
                        : Json(new { files = files.Take(1).ToArray(), nextPageToken = "page-two" });
                return Json(new { files });
            }
            if (uri.AbsolutePath == "/drive/v3/files" && request.Method == HttpMethod.Post)
            {
                using var json = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(ct), cancellationToken: ct);
                Assert.False(json.RootElement.TryGetProperty("permissions", out _));
                var metadata = json.RootElement;
                if (metadata.GetProperty("mimeType").GetString() == "application/vnd.google-apps.folder")
                    CreatedFolders.Add(metadata.GetProperty("name").GetString()!);
                else
                    Files.Add(metadata.GetProperty("id").GetString()!, new(metadata.GetProperty("name").GetString()!,
                        metadata.GetProperty("parents")[0].GetString()!, metadata.GetProperty("mimeType").GetString()!, []));
                return Json(new { id = metadata.GetProperty("id").GetString() });
            }
            if (uri.AbsolutePath == "/upload/drive/v3/files")
            {
                using var json = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(ct), cancellationToken: ct);
                _uploadId = json.RootElement.GetProperty("id").GetString()!;
                _upload = new(json.RootElement.GetProperty("name").GetString()!, json.RootElement.GetProperty("parents")[0].GetString()!,
                    json.RootElement.GetProperty("mimeType").GetString()!, []);
                _bytes.SetLength(0);
                var response = Json(new { });
                response.Headers.Location = new Uri("https://www.googleapis.com/session");
                return response;
            }
            if (uri.AbsolutePath == "/session")
            {
                var range = request.Content!.Headers.ContentRange!;
                if (!range.HasRange)
                {
                    StatusProbes++;
                    var status = Json(new { }, 308);
                    if (_bytes.Length > 0) status.Headers.TryAddWithoutValidation("Range", $"bytes=0-{_bytes.Length - 1}");
                    return status;
                }
                Chunks++;
                Assert.Equal(_bytes.Length, range.From);
                await request.Content.CopyToAsync(_bytes, ct);
                if (DisconnectAfterChunk)
                {
                    DisconnectAfterChunk = false;
                    throw new HttpRequestException("Simulated lost acknowledgement");
                }
                if (_bytes.Length == range.Length)
                {
                    Files[_uploadId] = _upload! with { Bytes = _bytes.ToArray() };
                    return Json(new { id = _uploadId });
                }
                var response = Json(new { }, 308);
                response.Headers.TryAddWithoutValidation("Range", $"bytes=0-{_bytes.Length - 1}");
                return response;
            }
            var id = uri.Segments.Last();
            if (request.Method == HttpMethod.Get && query.ContainsKey("alt"))
            {
                Downloads++;
                return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Files[id].Bytes) };
            }
            if (request.Method == HttpMethod.Delete) { Files.Remove(id); return new(HttpStatusCode.NoContent); }
            if (request.Method == HttpMethod.Patch)
            {
                using var json = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(ct), cancellationToken: ct);
                Files[id] = Files[id] with { Name = json.RootElement.GetProperty("name").GetString()! };
                return Json(new { id });
            }
            throw new InvalidOperationException($"Unexpected Drive test request: {request.Method} {uri.AbsolutePath}");
        }
        private static HttpResponseMessage Json(object body, int status = 200) => new((HttpStatusCode)status) { Content = JsonContent.Create(body) };
    }
}
