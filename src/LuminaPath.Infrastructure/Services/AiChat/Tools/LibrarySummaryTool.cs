using System.Text.Json;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat.Tools;

public sealed class LibrarySummaryTool : IChatTool
{
    public string Name => "library_summary";

    public string Description =>
        "High-level statistics about the user's library: total games, counts by status, total logged hours, " +
        "and estimated remaining backlog hours.";

    public JsonElement InputSchema { get; } = ChatToolJson.Schema("""
    {
      "type": "object",
      "properties": {}
    }
    """);

    public async Task<string> ExecuteAsync(JsonElement arguments, ChatToolContext context, CancellationToken cancellationToken)
    {
        var contextFactory = context.Services.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.MyGames.AsNoTracking()
            .Where(m => m.LuminaUserId == context.UserId)
            .Include(m => m.Game)
            .Select(m => new
            {
                m.Status,
                m.TimeSpend,
                EstimatedPlaytime = m.Game != null ? m.Game.Playtime : null,
            })
            .ToListAsync(cancellationToken);

        var byStatus = rows
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        foreach (var status in Enum.GetValues<GameStatus>())
        {
            byStatus.TryAdd(status.ToString(), 0);
        }

        var loggedHours = Math.Round(rows.Sum(r => r.TimeSpend ?? 0), 1);
        var remainingHours = Math.Round(
            rows.Sum(r => Math.Max(0, (r.EstimatedPlaytime ?? 0) - (r.TimeSpend ?? 0))), 1);

        return ChatToolJson.Serialize(new
        {
            total_games = rows.Count,
            by_status = byStatus,
            logged_hours = loggedHours,
            remaining_backlog_hours = remainingHours,
        });
    }
}
