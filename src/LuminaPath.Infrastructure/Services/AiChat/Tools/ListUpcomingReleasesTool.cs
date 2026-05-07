using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

public sealed class ListUpcomingReleasesTool : IChatTool
{
    public string Name => "list_upcoming_releases";

    public string Description =>
        "List games with a release date in the future, optionally filtered by month/year or by whether the game is in the user's library. " +
        "Returns up to `limit` games sorted by release date ascending.";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {
        "month": { "type": "integer", "description": "Optional 1-12 calendar month filter", "minimum": 1, "maximum": 12 },
        "year": { "type": "integer", "description": "Optional 4-digit year filter" },
        "only_my_library": { "type": "boolean", "description": "If true, only games the user has saved to their library", "default": false },
        "limit": { "type": "integer", "description": "Max results to return", "default": 25, "minimum": 1, "maximum": 100 }
      }
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var month = ChatToolJson.OptionalInt(arguments, "month");
        var year = ChatToolJson.OptionalInt(arguments, "year");
        var onlyMine = ChatToolJson.OptionalBool(arguments, "only_my_library") ?? false;
        var limit = Math.Clamp(ChatToolJson.OptionalInt(arguments, "limit") ?? 25, 1, 100);

        var contextFactory = context.Services.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var query = db.Games.AsNoTracking().Where(g => g.ReleaseDate.HasValue && g.ReleaseDate >= today);

        if (year.HasValue) query = query.Where(g => g.ReleaseDate!.Value.Year == year.Value);
        if (month.HasValue) query = query.Where(g => g.ReleaseDate!.Value.Month == month.Value);
        if (onlyMine)
        {
            query = query.Where(g => g.MyGames!.Any(m => m.LuminaUserId == context.UserId));
        }

        var rows = await query
            .OrderBy(g => g.ReleaseDate)
            .Take(limit)
            .Select(g => new
            {
                g.Id,
                g.Name,
                ReleaseDate = g.ReleaseDate!.Value.ToString("yyyy-MM-dd"),
                Platforms = g.Platforms.ToString(),
                g.Genres,
                InMyLibrary = g.MyGames!.Any(m => m.LuminaUserId == context.UserId),
            })
            .ToListAsync(cancellationToken);

        return ChatToolJson.Serialize(new { count = rows.Count, releases = rows });
    }
}
