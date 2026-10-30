using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Private S3 objects; only app routes are returned to clients. Buckets are provisioned by the operator.</summary>
public sealed class S3Storage(IAmazonS3 client, IOptions<S3Options> options, ILogger<S3Storage> logger) : IStorageService
{
    private readonly S3Options _options = options.Value;
    private const long PartSize = 8 * 1024 * 1024;
    private const long MaximumSize = 5L * 1024 * 1024 * 1024 * 1024;

    public async Task<List<BlobDto>> ListAsync()
    {
        var prefix = _options.AreaPrefix("uploads");
        var files = new List<BlobDto>();
        string? continuation = null;
        try
        {
            do
            {
                var page = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _options.BucketName, Prefix = prefix, ContinuationToken = continuation
                });
                foreach (var item in page.S3Objects ?? [])
                {
                    if (item.Key == prefix) continue; // Optional folder marker.
                    if (item.Key is null || !item.Key.StartsWith(prefix, StringComparison.Ordinal))
                        throw new IOException("S3 returned an object outside the configured upload prefix.");
                    var name = StorageFileNames.GetExactName(item.Key[prefix.Length..]);
                    files.Add(new BlobDto
                    {
                        Name = name,
                        Uri = AppUrl(name),
                        SizeBytes = item.Size,
                        LastModified = item.LastModified
                    });
                }
                if (page.IsTruncated != true) break;
                if (string.IsNullOrEmpty(page.NextContinuationToken) || page.NextContinuationToken == continuation)
                    throw new IOException("S3 returned an invalid listing continuation token.");
                continuation = page.NextContinuationToken;
            } while (true);
            return files;
        }
        catch (AmazonS3Exception ex) { throw ReadFailure("list", ex); }
    }

    public async Task<BlobDto?> DownloadAsync(string blobFilename)
    {
        var name = StorageFileNames.GetSafeName(blobFilename);
        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = _options.BucketName, Key = Key("uploads", name) });
            var temporary = TemporaryStream();
            try
            {
                await response.ResponseStream.CopyToAsync(temporary);
                temporary.Position = 0;
                return new BlobDto
                {
                    Name = name,
                    Uri = AppUrl(name),
                    ContentType = response.Headers.ContentType,
                    Content = temporary,
                    SizeBytes = response.Headers.ContentLength,
                    LastModified = response.LastModified
                };
            }
            catch { await temporary.DisposeAsync(); throw; }
        }
        catch (AmazonS3Exception ex) when (MissingObject(ex)) { return null; }
        catch (AmazonS3Exception ex) { throw ReadFailure("download", ex); }
    }

    public Task<BlobResponseDto> UploadAsync(Stream file, string fileName, string? contentType = null) =>
        UploadExactAsync(file, StorageFileNames.WithExtensionFromContentType(fileName, contentType), contentType);

    public Task<BlobResponseDto> UploadExactAsync(Stream file, string fileName, string? contentType = null,
        CancellationToken cancellationToken = default) => UploadAsync("uploads", file, fileName, contentType, cancellationToken);

    internal async Task<BlobResponseDto> UploadAsync(string area, Stream file, string fileName, string? contentType, CancellationToken ct)
    {
        var name = StorageFileNames.GetExactName(fileName);
        var key = Key(area, name);
        contentType ??= StorageContentTypes.GetContentType(name) ?? "application/octet-stream";
        FileStream? temporary = null;
        try
        {
            ct.ThrowIfCancellationRequested();
            if (!file.CanSeek)
            {
                temporary = TemporaryStream();
                await file.CopyToAsync(temporary, ct);
                temporary.Position = 0;
                file = temporary;
            }
            var start = file.Position;
            var length = file.Length - start;
            if (length > MaximumSize) throw new IOException("This storage adapter supports files up to 5 TiB.");
            if (length <= PartSize)
            {
                using var slice = new SliceStream(file, start, length);
                await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _options.BucketName, Key = key, InputStream = slice, ContentType = contentType,
                    IfNoneMatch = "*", AutoCloseStream = false, AutoResetStreamPosition = false,
                    UseChunkEncoding = string.IsNullOrWhiteSpace(_options.ServiceUrl)
                }, ct);
            }
            else await UploadMultipartAsync(file, start, length, key, contentType, ct);
            return new BlobResponseDto { Status = "File uploaded successfully.", Blob = new BlobDto
            { Name = name, ContentType = contentType, Uri = area == "uploads" ? AppUrl(name) : null } };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (AmazonS3Exception ex)
        {
            LogFailure("upload", ex);
            return new BlobResponseDto { Error = true, Status = "S3 upload failed. Check credentials, bucket access, connectivity or a conflicting filename." };
        }
        finally { if (temporary is not null) await temporary.DisposeAsync(); }
    }

    private async Task UploadMultipartAsync(Stream source, long start, long length, string key, string contentType, CancellationToken ct)
    {
        var initiated = await client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
        { BucketName = _options.BucketName, Key = key, ContentType = contentType }, ct);
        var completed = false;
        try
        {
            var parts = new List<PartETag>();
            var partSize = Math.Max(PartSize, (length + 9999) / 10000);
            for (long offset = 0; offset < length; offset += partSize)
            {
                using var slice = new SliceStream(source, start + offset, Math.Min(partSize, length - offset));
                var number = parts.Count + 1;
                var uploaded = await client.UploadPartAsync(new UploadPartRequest
                {
                    BucketName = _options.BucketName, Key = key, UploadId = initiated.UploadId,
                    PartNumber = number, PartSize = slice.Length, InputStream = slice, IsLastPart = offset + slice.Length == length
                }, ct);
                parts.Add(new PartETag(number, uploaded.ETag));
            }
            await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = _options.BucketName, Key = key, UploadId = initiated.UploadId,
                PartETags = parts, IfNoneMatch = "*"
            }, ct);
            completed = true;
        }
        finally
        {
            if (!completed)
            {
                // Cancellation of the transfer must not prevent cleanup of its unfinished parts.
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                    { BucketName = _options.BucketName, Key = key, UploadId = initiated.UploadId }, cleanup.Token); }
                catch (Exception ex) { LogFailure("abort multipart upload", ex); }
            }
        }
    }

    public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
    {
        try
        {
            await client.DeleteObjectAsync(new DeleteObjectRequest
            { BucketName = _options.BucketName, Key = Key("uploads", StorageFileNames.GetSafeName(blobFilename)) });
            return new BlobResponseDto { Status = "File deleted successfully." };
        }
        catch (AmazonS3Exception ex)
        {
            LogFailure("delete", ex);
            return new BlobResponseDto { Error = true, Status = "S3 deletion failed." };
        }
    }

    public async Task<bool> RenameAsync(string oldName, string newName)
    {
        oldName = StorageFileNames.GetSafeName(oldName);
        newName = StorageFileNames.ForRename(oldName, newName);
        try
        {
            using var original = await client.GetObjectAsync(new GetObjectRequest
            { BucketName = _options.BucketName, Key = Key("uploads", oldName) });
            if (oldName == newName) return true;
            if (string.IsNullOrEmpty(original.ETag)) throw new IOException("S3 did not return an object ETag for rename.");
            var copy = await UploadExactAsync(original.ResponseStream, newName, original.Headers.ContentType);
            if (copy.Error) return false;
            // Do not delete a source changed by another writer during the copy.
            await client.DeleteObjectAsync(new DeleteObjectRequest
            { BucketName = _options.BucketName, Key = Key("uploads", oldName), IfMatch = original.ETag });
            return true;
        }
        catch (Exception ex) when (ex is AmazonS3Exception or IOException)
        {
            LogFailure("rename", ex);
            return false;
        }
    }

    private string Key(string area, string name) => area is "uploads" or "backups"
        ? _options.AreaPrefix(area) + name : throw new ArgumentException("Unknown storage area.", nameof(area));
    private static string AppUrl(string name) => "api/files/" + Uri.EscapeDataString(name);
    private static bool MissingObject(AmazonS3Exception ex) => ex.StatusCode == HttpStatusCode.NotFound && ex.ErrorCode is "NoSuchKey" or "NotFound";
    private IOException ReadFailure(string operation, AmazonS3Exception ex)
    {
        LogFailure(operation, ex);
        return new IOException($"S3 {operation} failed (HTTP {(int)ex.StatusCode}). Check bucket access and connectivity.");
    }
    private void LogFailure(string operation, Exception ex) => logger.LogWarning("S3 {Operation} failed ({ErrorType}).", operation, ex.GetType().Name);
    private static FileStream TemporaryStream()
    {
        var settings = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose };
        if (!OperatingSystem.IsWindows()) settings.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), settings);
    }

    /// <summary>Seekable part view for SDK retries; disposing it never closes the caller's stream.</summary>
    private sealed class SliceStream(Stream source, long start, long length) : Stream
    {
        private long _position;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }
        public override long Seek(long offset, SeekOrigin origin)
        {
            var next = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => _position + offset,
                SeekOrigin.End => length + offset, _ => throw new ArgumentOutOfRangeException(nameof(origin)) };
            if (next < 0 || next > length) throw new IOException("Seek outside the upload part.");
            return _position = next;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            source.Position = start + _position;
            var read = source.Read(buffer, offset, (int)Math.Min(count, length - _position));
            _position += read;
            return read;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            source.Position = start + _position;
            var read = await source.ReadAsync(buffer[..(int)Math.Min(buffer.Length, length - _position)], cancellationToken);
            _position += read;
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
