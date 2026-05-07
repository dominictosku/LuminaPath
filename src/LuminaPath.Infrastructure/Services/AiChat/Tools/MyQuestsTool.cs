using System.Text.Json;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

public sealed class MyQuestsTool : IChatTool
{
    public string Name => "my_quests";

    public string Description =>
        "List quests for the current user. Filter by completion status. " +
        "Returns title, type, reward, and the linked game (if any).";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {
        "status": { "type": "string", "enum": ["all", "open", "completed"], "default": "open" },
        "limit": { "type": "integer", "default": 25, "minimum": 1, "maximum": 100 }
      }
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var status = ChatToolJson.OptionalString(arguments, "status") ?? "open";
        var limit = Math.Clamp(ChatToolJson.OptionalInt(arguments, "limit") ?? 25, 1, 100);

        var contextFactory = context.Services.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.Quests.AsNoTracking()
            .Where(q => q.LuminaUserId == context.UserId)
            .Include(q => q.MyGame!).ThenInclude(m => m.Game)
            .AsQueryable();

        query = status switch
        {
            "open" => query.Where(q => !q.Completed),
            "completed" => query.Where(q => q.Completed),
            _ => query,
        };

        var rows = await query
            .OrderBy(q => q.Completed)
            .ThenBy(q => q.SortOrder)
            .Take(limit)
            .Select(q => new
            {
                q.Title,
                Type = q.Type.ToString(),
                q.RewardXp,
                q.Completed,
                CompletedAt = q.CompletedAt.HasValue ? q.CompletedAt!.Value.ToString("yyyy-MM-dd") : null,
                Game = q.MyGame != null && q.MyGame.Game != null ? q.MyGame.Game.Name : null,
            })
            .ToListAsync(cancellationToken);

        return ChatToolJson.Serialize(new { count = rows.Count, quests = rows });
    }
}
