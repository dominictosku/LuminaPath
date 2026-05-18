using System.Security.Claims;
using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Auditing;

public sealed record AuditActor(string? UserId, string? Email)
{
    public static AuditActor System { get; } = new(null, "System");
}

public sealed class AuditLogEntry
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = AuditOutcomes.Success;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? TargetName { get; set; }
    public object? Changes { get; set; }
    public object? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public AuditActor? Actor { get; set; }
}

public sealed record AuditLogQuery(
    string? Category = null,
    string? Action = null,
    string? Outcome = null,
    string? Search = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 0,
    int PageSize = 50);

public sealed record AuditLogPage(IReadOnlyList<AuditLog> Items, int Total);

public sealed class AuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditLogService>? _logger;

    public AuditLogService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        : this(dbContextFactory, null, null)
    {
    }

    public AuditLogService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IHttpContextAccessor? httpContextAccessor,
        ILogger<AuditLogService>? logger)
    {
        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Category) || string.IsNullOrWhiteSpace(entry.Action))
        {
            return;
        }

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var httpContext = _httpContextAccessor?.HttpContext;
            var actor = entry.Actor ?? ResolveActor(httpContext);

            context.AuditLogs.Add(new AuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                Category = entry.Category.Trim(),
                Action = entry.Action.Trim(),
                Outcome = string.IsNullOrWhiteSpace(entry.Outcome) ? AuditOutcomes.Success : entry.Outcome.Trim(),
                ActorUserId = TrimOrNull(actor.UserId, 128),
                ActorEmail = TrimOrNull(actor.Email, 256),
                TargetType = TrimOrNull(entry.TargetType, 128),
                TargetId = TrimOrNull(entry.TargetId, 128),
                TargetName = TrimOrNull(entry.TargetName, 256),
                RequestPath = TrimOrNull(httpContext?.Request.Path.Value, 512),
                HttpMethod = TrimOrNull(httpContext?.Request.Method, 16),
                IpAddress = TrimOrNull(httpContext?.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = TrimOrNull(httpContext?.Request.Headers.UserAgent.ToString(), 512),
                CorrelationId = TrimOrNull(httpContext?.TraceIdentifier, 128),
                ChangesJson = SerializeOrNull(entry.Changes),
                MetadataJson = SerializeOrNull(entry.Metadata),
                ErrorMessage = TrimOrNull(entry.ErrorMessage, 2048)
            });

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not write audit log entry for {Category}/{Action}.", entry.Category, entry.Action);
        }
    }

    public async Task<AuditLogPage> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<AuditLog> logs = context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            logs = logs.Where(log => log.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(log => log.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            logs = logs.Where(log => log.Outcome == query.Outcome);
        }

        if (query.FromUtc.HasValue)
        {
            logs = logs.Where(log => log.TimestampUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            logs = logs.Where(log => log.TimestampUtc < query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            logs = logs.Where(log =>
                (log.ActorEmail != null && log.ActorEmail.Contains(search)) ||
                (log.TargetName != null && log.TargetName.Contains(search)) ||
                (log.TargetId != null && log.TargetId.Contains(search)) ||
                (log.RequestPath != null && log.RequestPath.Contains(search)));
        }

        var total = await logs.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(query.Page, 0);
        var items = await logs
            .OrderByDescending(log => log.TimestampUtc)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AuditLogPage(items, total);
    }

    public static AuditActor ActorFromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new AuditActor(null, null);
        }

        return new AuditActor(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue(ClaimTypes.Name) ?? principal.Identity?.Name);
    }

    public static Dictionary<string, object?> Changes(params (string Field, object? OldValue, object? NewValue)[] changes)
    {
        return changes
            .Where(change => !Equals(change.OldValue, change.NewValue))
            .ToDictionary(
                change => change.Field,
                change => (object?)new { old = change.OldValue, @new = change.NewValue });
    }

    public static string ValueState(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "empty" : "set";
    }

    private static AuditActor ResolveActor(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return AuditActor.System;
        }

        return ActorFromPrincipal(httpContext.User);
    }

    private static string? SerializeOrNull(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
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
