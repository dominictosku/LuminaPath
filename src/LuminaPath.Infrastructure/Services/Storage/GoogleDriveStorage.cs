using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class GoogleDriveStorage : IStorageService, IDisposable
{
    private readonly GoogleDriveClient _drive;
    private readonly GoogleDriveOptions _options;
    private readonly ILogger<GoogleDriveStorage> _logger;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private readonly object _cacheLock = new();
    private long _revision;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 * 1024 });

    public GoogleDriveStorage(GoogleDriveClient drive, IOptions<GoogleDriveOptions> options, ILogger<GoogleDriveStorage> logger)
    {
        _drive = drive;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<BlobDto>> ListAsync() => (await _drive.ListAsync("uploads"))
        .Select(file => new BlobDto { Name = file.Name, ContentType = file.MimeType, Uri = AppUrl(file.Name) }).ToList();

    public async Task<BlobDto?> DownloadAsync(string blobFilename)
    {
        var name = StorageFileNames.GetSafeName(blobFilename);
        if (_cache.TryGetValue<CachedFile>(name, out var cached) && cached is not null)
            return new BlobDto { Name = name, ContentType = cached.ContentType, Content = new MemoryStream(cached.Bytes, writable: false) };
        var revision = Interlocked.Read(ref _revision);
        var file = await _drive.FindAsync("uploads", name);
        if (file is null) return null;
        var temporary = CreateTemporaryStream();
        try
        {
            await _drive.DownloadAsync(file.Id, temporary);
            temporary.Position = 0;
            if (_options.CacheMinutes > 0 && temporary.Length <= 4 * 1024 * 1024)
            {
                var bytes = new byte[(int)temporary.Length];
                await temporary.ReadExactlyAsync(bytes);
                lock (_cacheLock)
                {
                    // A concurrent delete/rename must not be undone by a late download.
                    if (revision == _revision)
                        _cache.Set(name, new CachedFile(bytes, file.MimeType), new MemoryCacheEntryOptions
                        {
                            Size = bytes.Length, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheMinutes)
                        });
                }
                temporary.Position = 0;
            }
            // DeleteOnClose ties cleanup to the lifetime of the returned download stream.
            return new BlobDto { Name = name, ContentType = file.MimeType, Content = temporary };
        }
        catch { await temporary.DisposeAsync(); throw; }
    }

    public async Task<BlobResponseDto> UploadAsync(Stream file, string fileName, string? contentType = null)
    {
        var name = StorageFileNames.WithExtensionFromContentType(fileName, contentType);
        contentType ??= StorageContentTypes.GetContentType(name);
        await _writes.WaitAsync();
        FileStream? temporary = null;
        try
        {
            if (!file.CanSeek)
            {
                temporary = CreateTemporaryStream();
                await file.CopyToAsync(temporary);
                temporary.Position = 0;
                file = temporary;
            }
            await _drive.UploadAsync("uploads", name, file, contentType ?? "application/octet-stream");
            Invalidate(name);
            return new BlobResponseDto
            {
                Status = "File uploaded successfully.",
                Blob = new BlobDto { Name = name, ContentType = contentType, Uri = AppUrl(name) }
            };
        }
        catch (Exception ex)
        {
            LogFailure("upload", ex);
            return new BlobResponseDto { Error = true, Status = "Google Drive upload failed. Check authorization, quota, connectivity or duplicate filenames." };
        }
        finally
        {
            if (temporary is not null) await temporary.DisposeAsync();
            _writes.Release();
        }
    }

    public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
    {
        var name = StorageFileNames.GetSafeName(blobFilename);
        await _writes.WaitAsync();
        try
        {
            Invalidate(name);
            var file = await _drive.FindAsync("uploads", name);
            if (file is null) return new BlobResponseDto { Error = true, Status = "File not found." };
            await _drive.DeleteAsync(file.Id);
            return new BlobResponseDto { Status = "File deleted successfully." };
        }
        catch (Exception ex)
        {
            LogFailure("delete", ex);
            return new BlobResponseDto { Error = true, Status = "Google Drive deletion failed." };
        }
        finally { _writes.Release(); }
    }

    public async Task<bool> RenameAsync(string oldName, string newName)
    {
        oldName = StorageFileNames.GetSafeName(oldName);
        newName = StorageFileNames.ForRename(oldName, newName);
        await _writes.WaitAsync();
        try
        {
            var file = await _drive.FindAsync("uploads", oldName);
            if (file is null) return false;
            if (oldName == newName) return true;
            if (await _drive.FindAsync("uploads", newName) is not null) return false;
            await _drive.RenameAsync(file.Id, newName);
            Invalidate(oldName, newName);
            return true;
        }
        catch (Exception ex) { LogFailure("rename", ex); return false; }
        finally { _writes.Release(); }
    }

    private void Invalidate(params string[] names)
    {
        lock (_cacheLock)
        {
            Interlocked.Increment(ref _revision);
            foreach (var name in names) _cache.Remove(name);
        }
    }

    private void LogFailure(string operation, Exception error) =>
        _logger.LogWarning("Google Drive {Operation} failed ({ErrorType}).", operation, error.GetType().Name);
    private static string AppUrl(string name) => "api/files/" + Uri.EscapeDataString(name);
    private static FileStream CreateTemporaryStream() => new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
        FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    public void Dispose() { _cache.Dispose(); _writes.Dispose(); }
    private sealed record CachedFile(byte[] Bytes, string ContentType);
}
