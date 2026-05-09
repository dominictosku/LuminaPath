using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;

namespace LuminaPath.Core.Extensions;

public static class MediaExternalIdExtensions
{
    public static string? GetExternalId(this IEnumerable<MediaExternalId>? externalIds, ExternalMediaProvider provider)
    {
        return externalIds?
            .FirstOrDefault(externalId => externalId.Provider == provider)
            ?.ExternalId;
    }

    public static void SetExternalId(this ICollection<MediaExternalId> externalIds, ExternalMediaProvider provider, string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var existing = externalIds.FirstOrDefault(externalId => externalId.Provider == provider);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (existing is not null)
            {
                externalIds.Remove(existing);
            }

            return;
        }

        if (existing is null)
        {
            externalIds.Add(new MediaExternalId
            {
                Provider = provider,
                ExternalId = normalized
            });
            return;
        }

        existing.ExternalId = normalized;

        foreach (var duplicate in externalIds
            .Where(externalId => externalId.Provider == provider && externalId != existing)
            .ToList())
        {
            externalIds.Remove(duplicate);
        }
    }
}
