using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

public sealed class MyGamingSessionsTool : IChatTool
{
    public string Name => "my_gaming_sessions";

    public string Description =>
        "List the user's gaming sessions. By default returns upcoming sessions in the next 14 days. " +
        "Use `from`/`to` (ISO date) for a specific window, or `include_past=true` to include completed/past sessions.";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {
        "from": { "type": "string", "description": "ISO date (yyyy-MM-dd). Defaults to today." },
        "to": { "type": "string", "description": "ISO date (yyyy-MM-dd). Defaults to 14 days from now." },
        "include_past": { "type": "boolean", "default": false },
        "limit": { "type": "integer", "default": 25, "minimum": 1, "maximum": 100 }
      }
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var fromText = ChatToolJson.OptionalString(arguments, "from");
        var toText = ChatToolJson.OptionalString(arguments, "to");
        var includePast = ChatToolJson.OptionalBool(arguments, "include_past") ?? false;
        var limit = Math.Clamp(ChatToolJson.OptionalInt(arguments, "limit") ?? 25, 1, 100);

        DateTime? from = TryParseDate(fromText);
        DateTime? to = TryParseDate(toText);

        var today = DateTime.UtcNow.Date;
        from ??= includePast ? today.AddDays(-30) : today;
        to ??= today.AddDays(14);

        var contextFactory = context.Services.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.GamingSessions.AsNoTracking()
            .Where(s => s.LuminaUserId == context.UserId)
            .Where(s => s.ScheduledAt >= from && s.ScheduledAt <= to)
            .Include(s => s.MyGame!).ThenInclude(m => m.Game);

        var rows = await query
            .OrderBy(s => s.ScheduledAt)
            .Take(limit)
            .Select(s => new
            {
                ScheduledAt = s.ScheduledAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                s.DurationMinutes,
                s.Completed,
                s.Notes,
                Game = s.MyGame != null && s.MyGame.Game != null ? s.MyGame.Game.Name : null,
            })
            .ToListAsync(cancellationToken);

        return ChatToolJson.Serialize(new { count = rows.Count, sessions = rows });
    }

    private static DateTime? TryParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var parsed) ? parsed : null;
    }
}
