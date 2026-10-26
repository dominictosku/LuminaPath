using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace LuminaPath.Tests.Services.Storage;

public sealed class S3StorageTests
{
    [Fact]
    public async Task Lifecycle_UsesPrivateAppUrlsExactKeysAndConditionalWrites()
    {
        using var f = new Fixture();
        using var input = new MemoryStream([9, 1, 2]);
        input.Position = 1;
        var uploaded = await f.Storage.UploadAsync(input, "cover", "image/png");
        Assert.False(uploaded.Error);
        Assert.True(input.CanRead);
        Assert.Equal("api/files/cover.png", uploaded.Blob.Uri);
        Assert.Equal(new byte[] { 1, 2 }, f.Files["test/uploads/cover.png"].Bytes);
        var duplicate = await f.Storage.UploadExactAsync(new MemoryStream([8]), "cover.png", "image/png");
        Assert.True(duplicate.Error);
        Assert.Equal(new byte[] { 1, 2 }, f.Files["test/uploads/cover.png"].Bytes);
        var download = await f.Storage.DownloadAsync("cover.png");
        await using (var stream = download!.Content!)
        {
            Assert.Equal("image/png", download.ContentType);
            Assert.Equal(new byte[] { 1, 2 }, await Bytes(stream));
        }
        Assert.True(await f.Storage.RenameAsync("cover.png", "renamed"));
        Assert.False(f.Files.ContainsKey("test/uploads/cover.png"));
        Assert.Contains("test/uploads/renamed.png", f.Files.Keys);
        Assert.False((await f.Storage.DeleteAsync("renamed.png")).Error);
        Assert.Empty(await f.Storage.ListAsync());
    }

    [Fact]
    public async Task EmptyAndNonSeekableUploads_KeepNamesAndCallerStreamsOpen()
    {
        using var f = new Fixture();
        using var stream = new NonSeekableStream([3, 4]);
        Assert.False((await f.Storage.UploadExactAsync(stream, "no-extension", "image/png")).Error);
        Assert.True(stream.CanRead);
        Assert.False((await f.Storage.UploadExactAsync(new MemoryStream(), "empty", "application/x-custom")).Error);
        Assert.Empty(f.Files["test/uploads/empty"].Bytes);
        Assert.Contains("test/uploads/no-extension", f.Files.Keys);
    }

    [Fact]
    public async Task Listing_PaginatesAndKeepsBackupsOutsideUploads()
    {
        using var f = new Fixture();
        f.Files["test/uploads/a"] = new([1], "image/png");
        f.Files["test/uploads/b"] = new([2], "image/png");
        f.Files["test/backups/database.dump"] = new([3], "application/octet-stream");
        f.Files["other/uploads/private"] = new([4], "image/png");
        var listed = await f.Storage.ListAsync();
        Assert.Equal(new[] { "a", "b" }, listed.Select(file => file.Name));
        Assert.Equal(2, f.ListCalls);
        f.Files["test/uploads/nested/file"] = new([1], "image/png");
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Storage.ListAsync());
    }

    [Fact]
    public async Task MissingObjectsAreNull_ButAccessAndBucketErrorsStopMigration()
    {
        using var f = new Fixture();
        Assert.Null(await f.Storage.DownloadAsync("missing"));
        foreach (var (status, code) in new[] { (HttpStatusCode.Forbidden, "AccessDenied"), (HttpStatusCode.NotFound, "NoSuchBucket") })
        {
            f.Client.Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(Error(status, code));
            var error = await Assert.ThrowsAsync<IOException>(() => f.Storage.DownloadAsync("private"));
            Assert.DoesNotContain("sensitive-provider-error", error.ToString());
        }
    }

    [Fact]
    public async Task Multipart_PreservesBytesAndUsesConditionalCompletion()
    {
        using var f = new Fixture();
        var bytes = new byte[8 * 1024 * 1024 + 17];
        new Random(12).NextBytes(bytes);
        using var stream = new MemoryStream(bytes);
        Assert.False((await f.Storage.UploadExactAsync(stream, "large.bin")).Error);
        Assert.True(stream.CanRead);
        Assert.Equal(2, f.PartCalls);
        Assert.Equal(bytes, f.Files["test/uploads/large.bin"].Bytes);
        Assert.Equal(0, f.Aborts);
        Assert.True((await f.Storage.UploadExactAsync(new MemoryStream(bytes), "large.bin")).Error);
        Assert.Equal(1, f.Aborts);
        Assert.Equal(bytes, f.Files["test/uploads/large.bin"].Bytes);
    }

    [Fact]
    public async Task CancelledMultipart_AbortsWithIndependentCancellationToken()
    {
        using var f = new Fixture();
        using var cancellation = new CancellationTokenSource();
        f.Client.Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
            .Returns<UploadPartRequest, CancellationToken>((_, ct) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<UploadPartResponse>(ct);
            });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Storage.UploadExactAsync(
            new MemoryStream(new byte[8 * 1024 * 1024 + 1]), "cancelled.bin", null, cancellation.Token));
        Assert.Equal(1, f.Aborts);
        Assert.Empty(f.Files);
    }

    [Fact]
    public async Task Rename_LeavesSourceOnConflictAndChangedEtag()
    {
        using var f = new Fixture();
        f.Files["test/uploads/a.bin"] = new([1], "application/octet-stream");
        f.Files["test/uploads/b.bin"] = new([2], "application/octet-stream");
        Assert.False(await f.Storage.RenameAsync("a.bin", "b.bin"));
        Assert.Equal(new byte[] { 1 }, f.Files["test/uploads/a.bin"].Bytes);
        f.Client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Error(HttpStatusCode.PreconditionFailed, "PreconditionFailed"));
        Assert.False(await f.Storage.RenameAsync("a.bin", "c.bin"));
        Assert.Contains("test/uploads/a.bin", f.Files.Keys);
        Assert.Contains("test/uploads/c.bin", f.Files.Keys);
    }

    [Fact]
    public async Task Migration_RoundTripsAndBackupArchiveStaysSeparate()
    {
        using var f = new Fixture();
        var root = Path.Combine(Path.GetTempPath(), "s3-migration-" + Guid.NewGuid());
        try
        {
            var source = new FileSystemStorage(Path.Combine(root, "source"), NullLogger<FileSystemStorage>.Instance);
            var target = new FileSystemStorage(Path.Combine(root, "target"), NullLogger<FileSystemStorage>.Instance);
            await source.UploadExactAsync(new MemoryStream([1, 2, 3]), "cover", "image/png");
            var migration = new StorageMigrationService(NullLogger<StorageMigrationService>.Instance);
            Assert.Equal(1, (await migration.MigrateAsync(source, f.Storage, true)).Copied);
            Assert.Empty(f.Files);
            Assert.Equal(1, (await migration.MigrateAsync(source, f.Storage, false)).Copied);
            Assert.Equal(1, (await migration.MigrateAsync(source, f.Storage, false)).Skipped);
            var dump = Path.Combine(root, "database.dump");
            await File.WriteAllBytesAsync(dump, [9]);
            await new S3BackupArchive(f.Storage).SaveAsync(dump);
            Assert.Contains("test/backups/database.dump", f.Files.Keys);
            Assert.Equal(1, (await migration.MigrateAsync(f.Storage, target, false)).Copied);
            Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(Path.Combine(root, "target", "cover")));
            Assert.Single(await target.ListAsync());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task<byte[]> Bytes(Stream stream)
    {
        using var result = new MemoryStream();
        await stream.CopyToAsync(result);
        return result.ToArray();
    }
    private static AmazonS3Exception Error(HttpStatusCode status, string code) =>
        new("sensitive-provider-error") { StatusCode = status, ErrorCode = code };
    private sealed record Stored(byte[] Bytes, string Type);
    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
    private sealed class Fixture : IDisposable
    {
        public Mock<IAmazonS3> Client { get; } = new(MockBehavior.Strict);
        public Dictionary<string, Stored> Files { get; } = new(StringComparer.Ordinal);
        public S3Storage Storage { get; }
        public int ListCalls { get; private set; }
        public int PartCalls { get; private set; }
        public int Aborts { get; private set; }
        private readonly Dictionary<int, byte[]> _parts = new();
        private string _multipartType = "";
        public Fixture()
        {
            Storage = new S3Storage(Client.Object, Options.Create(new S3Options { BucketName = "bucket", Prefix = "test" }), NullLogger<S3Storage>.Instance);
            Client.Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ListObjectsV2Request request, CancellationToken _) =>
                {
                    ListCalls++;
                    Assert.Equal("test/uploads/", request.Prefix);
                    var objects = Files.Keys.Where(key => key.StartsWith(request.Prefix, StringComparison.Ordinal)).Order().ToList();
                    var offset = int.Parse(request.ContinuationToken ?? "0");
                    return new ListObjectsV2Response { S3Objects = objects.Skip(offset).Take(1).Select(key => new S3Object { Key = key }).ToList(),
                        IsTruncated = offset + 1 < objects.Count, NextContinuationToken = (offset + 1).ToString() };
                });
            Client.Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetObjectRequest request, CancellationToken _) =>
                {
                    if (!Files.TryGetValue(request.Key, out var value)) throw Error(HttpStatusCode.NotFound, "NoSuchKey");
                    var result = new GetObjectResponse { ResponseStream = new NonSeekableStream(value.Bytes), ETag = "original-etag" };
                    result.Headers.ContentType = value.Type;
                    return result;
                });
            Client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (PutObjectRequest request, CancellationToken _) =>
                {
                    Assert.Equal("*", request.IfNoneMatch);
                    Assert.False(request.AutoCloseStream);
                    Assert.False(request.AutoResetStreamPosition);
                    Assert.Null(request.CannedACL);
                    Assert.Equal("bucket", request.BucketName);
                    if (Files.ContainsKey(request.Key)) throw Error(HttpStatusCode.PreconditionFailed, "PreconditionFailed");
                    Files.Add(request.Key, new Stored(await Bytes(request.InputStream), request.ContentType));
                    return new PutObjectResponse();
                });
            Client.Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InitiateMultipartUploadRequest request, CancellationToken _) =>
                {
                    _parts.Clear();
                    _multipartType = request.ContentType;
                    Assert.Null(request.CannedACL);
                    return new InitiateMultipartUploadResponse { UploadId = "upload" };
                });
            Client.Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (UploadPartRequest request, CancellationToken _) =>
                {
                    PartCalls++;
                    // Exercise rewind as the SDK would on a retry.
                    request.InputStream.ReadByte();
                    request.InputStream.Seek(0, SeekOrigin.Begin);
                    var data = await Bytes(request.InputStream);
                    Assert.Equal(request.PartSize, data.LongLength);
                    _parts.Add(request.PartNumber!.Value, data);
                    return new UploadPartResponse { ETag = "part-" + request.PartNumber };
                });
            Client.Setup(c => c.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CompleteMultipartUploadRequest request, CancellationToken _) =>
                {
                    Assert.Equal("*", request.IfNoneMatch);
                    Assert.Equal(_parts.Count, request.PartETags.Count);
                    if (Files.ContainsKey(request.Key)) throw Error(HttpStatusCode.PreconditionFailed, "PreconditionFailed");
                    Files.Add(request.Key, new Stored(_parts.OrderBy(part => part.Key).SelectMany(part => part.Value).ToArray(), _multipartType));
                    return new CompleteMultipartUploadResponse();
                });
            Client.Setup(c => c.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AbortMultipartUploadRequest _, CancellationToken ct) =>
                {
                    Assert.False(ct.IsCancellationRequested);
                    Aborts++;
                    return new AbortMultipartUploadResponse();
                });
            Client.Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DeleteObjectRequest request, CancellationToken _) =>
                {
                    if (request.IfMatch is not null) Assert.Equal("original-etag", request.IfMatch);
                    Files.Remove(request.Key);
                    return new DeleteObjectResponse();
                });
        }
        public void Dispose() { }
    }
}
