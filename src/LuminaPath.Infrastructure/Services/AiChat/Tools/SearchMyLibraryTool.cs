using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

public sealed class SearchMyLibraryTool : IChatTool
{
    public string Name => "search_my_library";

    public string Description =>
        "Search games in the current user's library. Filter by name fragment, status (OnHold, Planned, Playing, StoryComplete, Completed, MainGame), or platform. " +
        "Returns playtime and progress for each match.";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {
        "name_contains": { "type": "string", "description": "Case-insensitive substring of the game name" },
        "status": { "type": "string", "enum": ["OnHold", "Planned", "Playing", "StoryComplete", "Completed", "MainGame"] },
        "platform": { "type": "string", "enum": ["Playstation4", "Playstation5", "Switch", "PC", "XBOX"] },
        "limit": { "type": "integer", "default": 25, "minimum": 1, "maximum": 100 }
      }
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var nameContains = ChatToolJson.OptionalString(arguments, "name_contains");
        var statusName = ChatToolJson.OptionalString(arguments, "status");
        var platformName = ChatToolJson.OptionalString(arguments, "platform");
        var limit = Math.Clamp(ChatToolJson.OptionalInt(arguments, "limit") ?? 25, 1, 100);

        var contextFactory = context.Services.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.MyGames.AsNoTracking()
            .Where(m => m.LuminaUserId == context.UserId)
            .Include(m => m.Game)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            var needle = nameContains.ToLower();
            query = query.Where(m => m.Game!.Name.ToLower().Contains(needle));
        }

        if (Enum.TryParse<Core.Enums.GameStatus>(statusName, ignoreCase: true, out var status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (Enum.TryParse<Core.Enums.Platforms>(platformName, ignoreCase: true, out var platform))
        {
            query = query.Where(m => (m.Game!.Platforms & platform) == platform);
        }

        var rows = await query
            .OrderByDescending(m => m.StartDate ?? DateTime.MinValue)
            .Take(limit)
            .Select(m => new
            {
                m.Game!.Name,
                Status = m.Status.ToString(),
                Platforms = m.Game!.Platforms.ToString(),
                EstimatedPlaytimeHours = m.Game!.Playtime,
                LoggedHours = m.TimeSpend ?? 0,
                ReleaseDate = m.Game!.ReleaseDate.HasValue ? m.Game.ReleaseDate!.Value.ToString("yyyy-MM-dd") : null,
                m.Rating,
                m.Priority,
                StartDate = m.StartDate.HasValue ? m.StartDate!.Value.ToString("yyyy-MM-dd") : null,
                EndDate = m.EndDate.HasValue ? m.EndDate!.Value.ToString("yyyy-MM-dd") : null,
            })
            .ToListAsync(cancellationToken);

        return ChatToolJson.Serialize(new { count = rows.Count, games = rows });
    }
}
