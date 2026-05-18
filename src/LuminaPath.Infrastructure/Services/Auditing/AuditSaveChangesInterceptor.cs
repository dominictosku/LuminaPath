using System.Text.Json;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LuminaPath.Infrastructure.Services.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
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
        ApplicationSettingsService.MetadataRawgApiKey
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

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is not LuminaPathDbContext dbContext)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();
        if (HasPendingInterceptorAudit(dbContext))
        {
            return;
        }

        var auditLogs = dbContext.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is not AuditLog)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(CreateAuditLog)
            .OfType<AuditLog>()
            .ToList();

        if (auditLogs.Count == 0)
        {
            return;
        }

        dbContext.AuditLogs.AddRange(auditLogs!);
    }

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        if (!TrackedProperties.TryGetValue(entry.Entity.GetType(), out var trackedProperties))
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
            if (entry.State == EntityState.Modified && !property.IsModified)
            {
                continue;
            }

            var oldValue = entry.State == EntityState.Added
                ? null
                : SanitizeValue(entry, property.Metadata.Name, property.OriginalValue);
            var newValue = entry.State == EntityState.Deleted
                ? null
                : SanitizeValue(entry, property.Metadata.Name, property.CurrentValue);

            if (Equals(oldValue, newValue))
            {
                continue;
            }

            changes[property.Metadata.Name] = new { old = oldValue, @new = newValue };
        }

        return changes;
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

    private static bool HasPendingInterceptorAudit(LuminaPathDbContext dbContext)
    {
        return dbContext.ChangeTracker.Entries<AuditLog>()
            .Any(entry => entry.State == EntityState.Added
                && entry.Entity.MetadataJson?.Contains("SaveChangesInterceptor", StringComparison.Ordinal) == true);
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
