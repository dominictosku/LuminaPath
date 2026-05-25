using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class QuestService
{
    private static async Task<int?> ResolveOwnedMyGameIdAsync(LuminaPathDbContext dbContext, string userId, int? myGameId)
    {
        if (!myGameId.HasValue)
        {
            return null;
        }

        var ownsGame = await dbContext.MyGames
            .AsNoTracking()
            .AnyAsync(myGame => myGame.Id == myGameId.Value && myGame.LuminaUserId == userId);

        return ownsGame ? myGameId.Value : null;
    }

    private static async Task<int?> ResolveOwnedSkillIdAsync(LuminaPathDbContext dbContext, string userId, int? skillId)
    {
        if (!skillId.HasValue)
        {
            return null;
        }

        var ownsSkill = await dbContext.QuestSkills
            .AsNoTracking()
            .AnyAsync(skill => skill.Id == skillId.Value && skill.LuminaUserId == userId);

        return ownsSkill ? skillId.Value : null;
    }

    private static async Task<int?> ResolveOwnedFolderIdAsync(LuminaPathDbContext dbContext, string userId, int? folderId)
    {
        if (!folderId.HasValue)
        {
            return null;
        }

        var ownsFolder = await dbContext.QuestFolders
            .AsNoTracking()
            .AnyAsync(folder => folder.Id == folderId.Value && folder.LuminaUserId == userId);

        return ownsFolder ? folderId.Value : null;
    }

    private static IQueryable<Quest> QueryQuestDetails(LuminaPathDbContext dbContext, string userId)
    {
        return dbContext.Quests
            .AsNoTracking()
            .Include(q => q.MyGame).ThenInclude(myGame => myGame!.Game)
            .Include(q => q.Skill)
            .Include(q => q.QuestFolder)
            .Include(q => q.Subtasks)
            .Where(q => q.LuminaUserId == userId);
    }

    private static async Task<QuestMutationResultDto> BuildMutationResultAsync(
        LuminaPathDbContext dbContext,
        string userId,
        int questId,
        int? spawnedId = null)
    {
        var quest = await QueryQuestDetails(dbContext, userId)
            .FirstAsync(q => q.Id == questId);

        QuestDto? spawned = null;
        if (spawnedId is int sid)
        {
            var spawnedEntity = await QueryQuestDetails(dbContext, userId)
                .FirstOrDefaultAsync(q => q.Id == sid);
            if (spawnedEntity != null)
            {
                spawned = ProjectQuestDto(spawnedEntity);
            }
        }

        var profile = await dbContext.QuestProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.LuminaUserId == userId);

        return new QuestMutationResultDto
        {
            Quest = ProjectQuestDto(quest),
            SpawnedQuest = spawned,
            TotalXp = profile?.TotalXp ?? 0,
            CurrentStreakDays = profile?.CurrentStreakDays ?? 0,
            LongestStreakDays = profile?.LongestStreakDays ?? 0
        };
    }

    private static async Task<Quest> SpawnNextRecurrenceAsync(LuminaPathDbContext dbContext, string userId, Quest source, DateTime now)
    {
        var anchor = source.DueDate ?? source.CompletedAt ?? now;
        var nextDue = source.Recurrence switch
        {
            QuestRecurrence.Daily => anchor.AddDays(1),
            QuestRecurrence.Weekly => anchor.AddDays(7),
            QuestRecurrence.Monthly => anchor.AddMonths(1),
            _ => anchor
        };

        var nextSort = await dbContext.Quests
            .Where(q => q.LuminaUserId == userId && q.Type == source.Type)
            .Select(q => (int?)q.SortOrder)
            .MaxAsync() ?? -1;

        var clone = new Quest
        {
            LuminaUserId = userId,
            Title = source.Title,
            Notes = source.Notes,
            Type = source.Type,
            Priority = source.Priority,
            Recurrence = source.Recurrence,
            DueDate = NormalizeDate(nextDue),
            Tags = new List<string>(source.Tags ?? new List<string>()),
            RewardXp = source.RewardXp,
            Completed = false,
            CreatedAt = now,
            UpdatedAt = now,
            SortOrder = nextSort + 1,
            MyGameId = source.MyGameId,
            SkillId = source.SkillId
        };

        await dbContext.Quests.AddAsync(clone);
        return clone;
    }

    private static async Task<QuestProfile> GetOrCreateProfileAsync(LuminaPathDbContext dbContext, string userId, DateTime now)
    {
        var profile = await dbContext.QuestProfiles
            .FirstOrDefaultAsync(p => p.LuminaUserId == userId);

        if (profile != null)
        {
            return profile;
        }

        profile = new QuestProfile
        {
            LuminaUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await dbContext.QuestProfiles.AddAsync(profile);
        return profile;
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value;
        return date.Kind == DateTimeKind.Utc
            ? date
            : DateTime.SpecifyKind(date.ToUniversalTime(), DateTimeKind.Utc);
    }

    private static List<string> NormalizeTags(List<string> tags)
    {
        return (tags ?? new())
            .Select(tag => (tag ?? string.Empty).Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static QuestDto ProjectQuestDto(Quest quest)
    {
        return new QuestDto
        {
            Id = quest.Id,
            Title = quest.Title,
            Notes = quest.Notes,
            Type = quest.Type,
            Priority = quest.Priority,
            Recurrence = quest.Recurrence,
            DueDate = quest.DueDate,
            Tags = quest.Tags ?? new(),
            RewardXp = quest.RewardXp,
            Completed = quest.Completed,
            CompletedAt = quest.CompletedAt,
            CreatedAt = quest.CreatedAt,
            UpdatedAt = quest.UpdatedAt,
            SortOrder = quest.SortOrder,
            MyGameId = quest.MyGameId,
            GameName = quest.MyGame == null ? null : quest.MyGame.Game!.Name,
            SkillId = quest.SkillId,
            SkillName = quest.Skill == null ? null : quest.Skill.Name,
            QuestFolderId = quest.QuestFolderId,
            FolderName = quest.QuestFolder?.Name,
            FolderEmoji = quest.QuestFolder?.Emoji,
            Subtasks = (quest.Subtasks ?? new())
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id)
                .Select(s => new QuestSubtaskDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Completed = s.Completed,
                    CompletedAt = s.CompletedAt,
                    SortOrder = s.SortOrder
                })
                .ToList()
        };
    }

    private static AchievementDto ProjectAchievementDto(Achievement achievement)
    {
        var def = AchievementCatalog.TryGet(achievement.Code);
        return new AchievementDto
        {
            Code = achievement.Code,
            Title = def?.Title ?? achievement.Code,
            Description = def?.Description ?? string.Empty,
            Icon = def?.Icon ?? "trophy-outline",
            UnlockedAt = achievement.UnlockedAt
        };
    }

    private static async Task<List<QuestDto>> LoadQuestDtos(LuminaPathDbContext dbContext, string userId)
    {
        var quests = await QueryQuestDetails(dbContext, userId)
            .OrderBy(quest => quest.SortOrder)
            .ThenBy(quest => quest.Id)
            .ToListAsync();

        return quests.Select(ProjectQuestDto).ToList();
    }

    private static int RewardFor(QuestType type)
    {
        return type switch
        {
            QuestType.Main => 150,
            QuestType.Faction => 100,
            _ => 75
        };
    }

    private static int SkillRewardFor(QuestType type)
    {
        return type switch
        {
            QuestType.Main => 30,
            QuestType.Faction => 20,
            _ => 15
        };
    }

    private static void UpdateStreakOnCompletion(QuestProfile profile, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var last = profile.LastCompletionDate;

        if (last == today)
        {
            return;
        }

        if (last is DateOnly previous && previous.AddDays(1) == today)
        {
            profile.CurrentStreakDays = Math.Max(1, profile.CurrentStreakDays + 1);
        }
        else
        {
            profile.CurrentStreakDays = 1;
        }

        profile.LastCompletionDate = today;
        if (profile.CurrentStreakDays > profile.LongestStreakDays)
        {
            profile.LongestStreakDays = profile.CurrentStreakDays;
        }
    }
}
