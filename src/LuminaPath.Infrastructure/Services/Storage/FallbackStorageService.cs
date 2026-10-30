using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>
/// Composite storage service that routes write operations to a primary provider,
/// while allowing reads to transparently fall back to a secondary provider if the
/// file has not yet been copied to the primary provider (e.g. during migration windows).
/// </summary>
public sealed class FallbackStorageService : IStorageService
{
    private readonly IStorageService _primary;
    private readonly IStorageService? _fallback;
    private readonly ILogger<FallbackStorageService> _logger;

    public FallbackStorageService(
        IStorageService primary,
        IStorageService? fallback,
        ILogger<FallbackStorageService> logger)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback;
        _logger = logger;
    }

    public Task<BlobResponseDto> UploadAsync(Stream file, string fileName, string? contentType = null) =>
        _primary.UploadAsync(file, fileName, contentType);

    public Task<BlobResponseDto> UploadExactAsync(Stream file, string fileName, string? contentType = null, CancellationToken cancellationToken = default) =>
        _primary.UploadExactAsync(file, fileName, contentType, cancellationToken);

    public async Task<BlobDto?> DownloadAsync(string blobFilename)
    {
        var blob = await _primary.DownloadAsync(blobFilename);
        if (blob is not null)
        {
            return blob;
        }

        if (_fallback is not null)
        {
            _logger.LogDebug("File '{FileName}' not found on primary storage provider; attempting fallback provider.", blobFilename);
            var fallbackBlob = await _fallback.DownloadAsync(blobFilename);
            if (fallbackBlob is not null)
            {
                _logger.LogInformation("File '{FileName}' served from fallback storage provider.", blobFilename);
                return fallbackBlob;
            }
        }

        return null;
    }

    public Task<BlobResponseDto> DeleteAsync(string blobFilename) =>
        _primary.DeleteAsync(blobFilename);

    public async Task<List<BlobDto>> ListAsync()
    {
        var primaryList = await _primary.ListAsync();
        if (_fallback is null)
        {
            return primaryList;
        }

        var names = new HashSet<string>(primaryList.Where(b => b.Name is not null).Select(b => b.Name!), StringComparer.Ordinal);
        var fallbackList = await _fallback.ListAsync();
        foreach (var item in fallbackList)
        {
            if (item.Name is not null && names.Add(item.Name))
            {
                primaryList.Add(item);
            }
        }

        return primaryList;
    }

    public Task<bool> RenameAsync(string oldName, string newName) =>
        _primary.RenameAsync(oldName, newName);
}
