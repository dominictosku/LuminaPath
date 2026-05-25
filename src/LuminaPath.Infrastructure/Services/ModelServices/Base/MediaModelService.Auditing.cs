using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

public abstract partial class MediaModelService<TMedia, TUserMedia> : GenericModelService<TMedia>
    where TMedia : Media
    where TUserMedia : MyMedia, IMyMedia
{
    private async Task<TMedia?> GetMediaSnapshotAsync(int id)
    {
        await using var context = await GetDbContextAsync();
        return await context.Set<TMedia>()
            .AsNoTracking()
            .FirstOrDefaultAsync(media => media.Id == id);
    }

    private static Dictionary<string, object?> MediaChanges(TMedia existing, TMedia updated)
    {
        return AuditLogService.Changes(
            ("Name", existing.Name, updated.Name),
            ("Description", existing.Description, updated.Description),
            ("ReleaseDate", existing.ReleaseDate, updated.ReleaseDate),
            ("Source", existing.Source, updated.Source),
            ("Genres", FormatGenres(existing.Genres), FormatGenres(updated.Genres)));
    }

    private static string FormatGenres(IEnumerable<string>? genres)
    {
        return genres is null ? string.Empty : string.Join(", ", genres);
    }

    private static string FailureMessage(FailedResult failure)
    {
        return string.Join("; ", failure.errorMessage);
    }

    private Task AuditMediaAsync(
        string action,
        string outcome,
        TMedia media,
        string? errorMessage = null,
        object? changes = null)
    {
        return AuditMediaAsync(
            action,
            outcome,
            media.Id > 0 ? media.Id.ToString() : null,
            media.Name,
            errorMessage,
            changes);
    }

    private Task AuditMediaAsync(
        string action,
        string outcome,
        string? targetId,
        string? targetName,
        string? errorMessage = null,
        object? changes = null)
    {
        if (_auditLog is null)
        {
            return Task.CompletedTask;
        }

        return _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Media,
            Action = action,
            Outcome = outcome,
            TargetType = typeof(TMedia).Name,
            TargetId = targetId,
            TargetName = targetName,
            Changes = changes,
            Metadata = new
            {
                source = "MediaModelService",
                mediaType = typeof(TMedia).Name
            },
            ErrorMessage = errorMessage
        });
    }
}
