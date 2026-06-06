using System.Text.Json;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LuminaPath.Infrastructure.Services.Auditing;

public sealed partial class AuditSaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitiveSettingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationSettingsService.SteamApiKey,
        ApplicationSettingsService.PsnBearerToken,
        ApplicationSettingsService.MetadataIgdbClientSecret,
        ApplicationSettingsService.MetadataRawgApiKey,
        ApplicationSettingsService.GoogleCalendarClientSecret,
        ApplicationSettingsService.AnthropicApiKey,
        ApplicationSettingsService.OpenAiApiKey
    };

    private static readonly Dictionary<Type, HashSet<string>> TrackedProperties = new()
    {
        [typeof(ApplicationSetting)] = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(ApplicationSetting.Value)
        },
        [typeof(BackgroundJobRecord)] = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(BackgroundJobRecord.Status),
            nameof(BackgroundJobRecord.StartedAt),
            nameof(BackgroundJobRecord.CompletedAt),
            nameof(BackgroundJobRecord.ResultMessage),
            nameof(BackgroundJobRecord.ErrorMessage)
        },
        [typeof(LuminaUser)] = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(LuminaUser.Email),
            nameof(LuminaUser.UserName),
            nameof(LuminaUser.FullName),
            nameof(LuminaUser.PhoneNumber),
            nameof(LuminaUser.EmailConfirmed),
            nameof(LuminaUser.LockoutEnabled),
            nameof(LuminaUser.LockoutEnd),
            nameof(LuminaUser.PasswordHash)
        },
        [typeof(IdentityUserRole<string>)] = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(IdentityUserRole<string>.UserId),
            nameof(IdentityUserRole<string>.RoleId)
        }
    };

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var trackedProperties = GetTrackedProperties(entry);
        if (trackedProperties is null)
        {
            return null;
        }

        var changes = CreateChanges(entry, trackedProperties);
        if (changes.Count == 0)
        {
            return null;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var actor = httpContext is null
            ? AuditActor.System
            : AuditLogService.ActorFromPrincipal(httpContext.User);

        return new AuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            Category = AuditCategories.System,
            Action = AuditActions.EntityChanged,
            Outcome = AuditOutcomes.Success,
            ActorUserId = TrimOrNull(actor.UserId, 128),
            ActorEmail = TrimOrNull(actor.Email, 256),
            TargetType = TrimOrNull(GetTargetType(entry), 128),
            TargetId = TrimOrNull(GetTargetId(entry), 128),
            TargetName = TrimOrNull(GetTargetName(entry), 256),
            RequestPath = TrimOrNull(httpContext?.Request.Path.Value, 512),
            HttpMethod = TrimOrNull(httpContext?.Request.Method, 16),
            IpAddress = TrimOrNull(httpContext?.Connection.RemoteIpAddress?.ToString(), 64),
            UserAgent = TrimOrNull(httpContext?.Request.Headers.UserAgent.ToString(), 512),
            CorrelationId = TrimOrNull(httpContext?.TraceIdentifier, 128),
            ChangesJson = JsonSerializer.Serialize(changes, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(new
            {
                source = "SaveChangesInterceptor",
                entityState = entry.State.ToString()
            }, JsonOptions)
        };
    }

    private static Dictionary<string, object?> CreateChanges(EntityEntry entry, HashSet<string> trackedProperties)
    {
        var changes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties.Where(property => trackedProperties.Contains(property.Metadata.Name)))
        {
            var oldValue = entry.State == EntityState.Added
                ? null
                : SanitizeValue(entry, property.Metadata.Name, property.OriginalValue);
            var newValue = entry.State == EntityState.Deleted
                ? null
                : SanitizeValue(entry, property.Metadata.Name, property.CurrentValue);

            if (entry.State == EntityState.Modified && !property.IsModified && Equals(oldValue, newValue))
            {
                continue;
            }

            changes[property.Metadata.Name] = new { old = oldValue, @new = newValue };
        }

        return changes;
    }

    private static HashSet<string>? GetTrackedProperties(EntityEntry entry)
    {
        return entry.Entity switch
        {
            ApplicationSetting => TrackedProperties[typeof(ApplicationSetting)],
            BackgroundJobRecord => TrackedProperties[typeof(BackgroundJobRecord)],
            LuminaUser => TrackedProperties[typeof(LuminaUser)],
            IdentityUserRole<string> => TrackedProperties[typeof(IdentityUserRole<string>)],
            _ => null
        };
    }

    private static object? SanitizeValue(EntityEntry entry, string propertyName, object? value)
    {
        if (entry.Entity is ApplicationSetting setting
            && propertyName == nameof(ApplicationSetting.Value)
            && SensitiveSettingKeys.Contains(setting.Key))
        {
            return AuditLogService.ValueState(value?.ToString());
        }

        return propertyName == nameof(LuminaUser.PasswordHash)
            ? AuditLogService.ValueState(value?.ToString())
            : value;
    }

    private static string GetTargetType(EntityEntry entry)
    {
        return entry.Entity switch
        {
            ApplicationSetting => nameof(ApplicationSetting),
            BackgroundJobRecord => nameof(BackgroundJobRecord),
            LuminaUser => nameof(LuminaUser),
            IdentityUserRole<string> => "IdentityUserRole",
            _ => entry.Metadata.ClrType.Name
        };
    }

    private static string? GetTargetId(EntityEntry entry)
    {
        return entry.Entity switch
        {
            ApplicationSetting setting => setting.Key,
            BackgroundJobRecord job => job.Id > 0 ? job.Id.ToString() : null,
            LuminaUser user => user.Id,
            IdentityUserRole<string> role => $"{role.UserId}:{role.RoleId}",
            _ => null
        };
    }

    private static string? GetTargetName(EntityEntry entry)
    {
        return entry.Entity switch
        {
            ApplicationSetting setting => setting.Key,
            BackgroundJobRecord job => job.DisplayName,
            LuminaUser user => user.Email ?? user.UserName,
            IdentityUserRole<string> role => role.UserId,
            _ => null
        };
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
