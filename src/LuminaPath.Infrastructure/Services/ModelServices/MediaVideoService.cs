using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public class MediaVideoService
{
    public const long MaxVideoUploadBytes = 256L * 1024L * 1024L;

    private static readonly IReadOnlyDictionary<string, VideoFileType> VideoTypesByExtension =
        new Dictionary<string, VideoFileType>(StringComparer.OrdinalIgnoreCase)
        {
            [".m4v"] = new("video/x-m4v", ".m4v"),
            [".mov"] = new("video/quicktime", ".mov"),
            [".mp4"] = new("video/mp4", ".mp4"),
            [".webm"] = new("video/webm", ".webm"),
        };

    private static readonly IReadOnlyDictionary<string, VideoFileType> VideoTypesByContentType =
        VideoTypesByExtension.Values
            .GroupBy(type => type.ContentType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IStorageService _storage;
    private readonly ILogger<MediaVideoService> _logger;

    public MediaVideoService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IStorageService storage,
        ILogger<MediaVideoService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaVideoDto>> GetForMediaAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var videos = await context.MediaVideos
            .AsNoTracking()
            .Where(video => video.MediaId == mediaId)
            .OrderBy(video => video.SortOrder)
            .ThenBy(video => video.Id)
            .ToListAsync(cancellationToken);

        return videos.Select(ToDto).ToList();
    }

    public async Task<Result<MediaVideoDto, FailedResult>> UploadAsync(
        int mediaId,
        Stream stream,
        string fileName,
        string? contentType,
        long sizeBytes,
        MediaVideoKind kind,
        string? title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (mediaId <= 0)
        {
            return new FailedResult("Media id is required.");
        }

        if (sizeBytes <= 0)
        {
            return new FailedResult("No video file was uploaded.");
        }

        if (sizeBytes > MaxVideoUploadBytes)
        {
            return new FailedResult($"Video is too large. Maximum allowed size is {MaxVideoUploadBytes / 1024 / 1024} MiB.");
        }

        var videoType = ResolveVideoType(fileName, contentType);
        if (videoType is null)
        {
            return new FailedResult("Only mp4, webm, mov, and m4v videos are allowed.");
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var mediaExists = await context.Set<Media>()
            .AsNoTracking()
            .AnyAsync(media => media.Id == mediaId, cancellationToken);
        if (!mediaExists)
        {
            return new FailedResult("Media item was not found.");
        }

        var displayFileName = GetDisplayFileName(fileName);
        var storageName = $"{Guid.NewGuid():N}{videoType.Extension}";
        var upload = await _storage.UploadAsync(stream, storageName, videoType.ContentType);
        if (upload.Error)
        {
            return new FailedResult(upload.Status ?? "Could not upload video.");
        }

        var sortOrder = await context.MediaVideos
            .Where(video => video.MediaId == mediaId)
            .MaxAsync(video => (int?)video.SortOrder, cancellationToken) ?? 0;

        var video = new MediaVideo
        {
            MediaId = mediaId,
            Title = ResolveTitle(title, displayFileName),
            Description = NormalizeDescription(description),
            Kind = kind,
            SortOrder = sortOrder + 1,
            FileName = displayFileName,
            StorageName = upload.Blob.Name ?? storageName,
            ContentType = upload.Blob.ContentType ?? videoType.ContentType,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        context.MediaVideos.Add(video);
        await context.SaveChangesAsync(cancellationToken);

        return ToDto(video);
    }

    public async Task<Result<MediaVideoDto, FailedResult>> UpdateAsync(
        int id,
        MediaVideoUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var video = await context.MediaVideos.FirstOrDefaultAsync(video => video.Id == id, cancellationToken);
        if (video is null)
        {
            return new FailedResult("Video was not found.");
        }

        video.Title = ResolveTitle(update.Title, video.FileName);
        video.Description = NormalizeDescription(update.Description);
        video.Kind = update.Kind;
        video.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return ToDto(video);
    }

    public async Task<Result<IReadOnlyList<MediaVideoDto>, FailedResult>> ReorderAsync(
        MediaVideoReorderDto reorder,
        CancellationToken cancellationToken = default)
    {
        if (reorder.MediaId <= 0)
        {
            return new FailedResult("Media id is required.");
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var videos = await context.MediaVideos
            .Where(video => video.MediaId == reorder.MediaId)
            .ToListAsync(cancellationToken);

        var orderedIds = reorder.VideoIds.Distinct().ToList();
        var knownIds = videos.Select(video => video.Id).ToHashSet();
        if (orderedIds.Count != videos.Count || orderedIds.Any(id => !knownIds.Contains(id)))
        {
            return new FailedResult("Video order must include every video for this media item.");
        }

        var byId = videos.ToDictionary(video => video.Id);
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var video = byId[orderedIds[index]];
            video.SortOrder = index + 1;
            video.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return videos
            .OrderBy(video => video.SortOrder)
            .ThenBy(video => video.Id)
            .Select(ToDto)
            .ToList();
    }

    public async Task<Result<int, FailedResult>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var video = await context.MediaVideos.FirstOrDefaultAsync(video => video.Id == id, cancellationToken);
        if (video is null)
        {
            return new FailedResult("Video was not found.");
        }

        var storageName = video.StorageName;
        context.MediaVideos.Remove(video);
        await context.SaveChangesAsync(cancellationToken);

        var delete = await _storage.DeleteAsync(storageName);
        if (delete.Error)
        {
            _logger.LogWarning("Could not delete video file {StorageName}: {Status}", storageName, delete.Status);
        }

        return id;
    }

    public async Task<MediaVideoStream?> GetStreamAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var video = await context.MediaVideos
            .AsNoTracking()
            .FirstOrDefaultAsync(video => video.Id == id, cancellationToken);
        if (video is null)
        {
            return null;
        }

        var blob = await _storage.DownloadAsync(video.StorageName);
        if (blob?.Content is null)
        {
            return null;
        }

        return new MediaVideoStream(
            blob.Content,
            video.ContentType,
            video.FileName);
    }

    private static MediaVideoDto ToDto(MediaVideo video)
    {
        return new MediaVideoDto
        {
            Id = video.Id,
            MediaId = video.MediaId,
            Title = video.Title,
            Description = video.Description,
            Kind = video.Kind,
            SortOrder = video.SortOrder,
            FileName = video.FileName,
            StorageName = video.StorageName,
            ContentType = video.ContentType,
            SizeBytes = video.SizeBytes,
            DurationSeconds = video.DurationSeconds,
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }

    private static VideoFileType? ResolveVideoType(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension)
            && VideoTypesByExtension.TryGetValue(extension, out var extensionMatch))
        {
            return extensionMatch;
        }

        if (!string.IsNullOrWhiteSpace(contentType)
            && VideoTypesByContentType.TryGetValue(contentType.Trim(), out var contentTypeMatch))
        {
            return contentTypeMatch;
        }

        return null;
    }

    private static string GetDisplayFileName(string fileName)
    {
        var displayName = Path.GetFileName(fileName.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(displayName) ? "video" : displayName.Trim();
    }

    private static string ResolveTitle(string? title, string fileName)
    {
        var normalized = title?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized.Length > 120 ? normalized[..120] : normalized;
        }

        var fromFile = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (string.IsNullOrWhiteSpace(fromFile))
        {
            return "Video";
        }

        return fromFile.Length > 120 ? fromFile[..120] : fromFile;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > 500 ? normalized[..500] : normalized;
    }

    private sealed record VideoFileType(string ContentType, string Extension);
}

public sealed record MediaVideoStream(Stream Content, string ContentType, string FileName);
