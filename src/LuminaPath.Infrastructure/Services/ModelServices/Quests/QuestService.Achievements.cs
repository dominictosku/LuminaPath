using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class QuestService
{
    private static async Task<List<Achievement>> EvaluateAchievementsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        QuestProfile profile,
        Quest justCompleted,
        DateTime now)
    {
        var already = await dbContext.Achievements
            .AsNoTracking()
            .Where(a => a.QuestProfileId == profile.Id)
            .Select(a => a.Code)
            .ToListAsync();
        var alreadySet = new HashSet<string>(already, StringComparer.OrdinalIgnoreCase);

        var completedCount = await dbContext.Quests
            .AsNoTracking()
            .CountAsync(q => q.LuminaUserId == userId && q.Completed);

        var newlyUnlocked = new List<Achievement>();
        void Try(string code)
        {
            if (!alreadySet.Contains(code))
            {
                newlyUnlocked.Add(new Achievement
                {
                    Code = code,
                    UnlockedAt = now,
                    QuestProfileId = profile.Id
                });
                alreadySet.Add(code);
            }
        }

        if (completedCount >= 1) Try(AchievementCatalog.FirstQuest);
        if (completedCount >= 10) Try(AchievementCatalog.TenQuests);
        if (completedCount >= 100) Try(AchievementCatalog.HundredQuests);

        if (justCompleted.Type == QuestType.Main)
        {
            Try(AchievementCatalog.FirstMainQuest);
        }

        if (justCompleted.Recurrence != QuestRecurrence.None)
        {
            Try(AchievementCatalog.DailyDiscipline);
        }

        if (justCompleted.Subtasks?.Count > 0 && justCompleted.Subtasks.All(s => s.Completed))
        {
            Try(AchievementCatalog.Completionist);
        }

        if (profile.CurrentStreakDays >= 7) Try(AchievementCatalog.SevenDayStreak);
        if (profile.CurrentStreakDays >= 30) Try(AchievementCatalog.ThirtyDayStreak);

        if (newlyUnlocked.Count > 0)
        {
            await dbContext.Achievements.AddRangeAsync(newlyUnlocked);
            await dbContext.SaveChangesAsync();
        }

        return newlyUnlocked;
    }
}
