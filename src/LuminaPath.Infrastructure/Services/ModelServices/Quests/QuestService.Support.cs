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
            .Include(q => q.QuestSeries)
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

    private static async Task<Quest?> SpawnNextRecurrenceAsync(LuminaPathDbContext dbContext, string userId, Quest source, DateTime now)
    {
        var series = source.QuestSeriesId.HasValue || source.QuestSeries != null
            ? await EnsureQuestSeriesAsync(dbContext, userId, source, now)
            : null;
        var recurrence = series?.Recurrence ?? source.Recurrence;
        if (recurrence == QuestRecurrence.None)
        {
            return null;
        }

        if (series == null)
        {
            series = await EnsureQuestSeriesAsync(dbContext, userId, source, now);
        }

        var anchor = series.DueDate ?? source.DueDate ?? source.CompletedAt ?? now;
        var nextScheduledStart = series.ScheduledStartAt.HasValue
            ? ShiftForRecurrence(series.ScheduledStartAt.Value, recurrence)
            : (DateTime?)null;
        var nextScheduledEnd = series.ScheduledEndAt.HasValue
            ? ShiftForRecurrence(series.ScheduledEndAt.Value, recurrence)
            : (DateTime?)null;
        var nextDue = nextScheduledStart.HasValue
            ? ScheduleDueDate(nextScheduledStart)
            : NormalizeDate(ShiftForRecurrence(anchor, recurrence));

        if (nextDue.HasValue)
        {
            var occurrenceDay = nextDue.Value.Date;
            var nextDay = occurrenceDay.AddDays(1);
            var existingOccurrence = await dbContext.Quests
                .FirstOrDefaultAsync(q =>
                    q.LuminaUserId == userId
                    && q.QuestSeriesId == series.Id
                    && q.SeriesOccurrenceDate.HasValue
                    && q.SeriesOccurrenceDate.Value >= occurrenceDay
                    && q.SeriesOccurrenceDate.Value < nextDay);

            if (existingOccurrence != null)
            {
                existingOccurrence.ProjectsQuestSeries = true;
                existingOccurrence.UpdatedAt = now;
                series.DueDate = nextDue;
                series.ScheduledStartAt = nextScheduledStart;
                series.ScheduledEndAt = nextScheduledEnd;
                series.UpdatedAt = now;
                return existingOccurrence;
            }
        }

        var nextSort = await dbContext.Quests
            .Where(q => q.LuminaUserId == userId && q.Type == series.Type)
            .Select(q => (int?)q.SortOrder)
            .MaxAsync() ?? -1;

        var clone = new Quest
        {
            LuminaUserId = userId,
            Title = series.Title,
            Notes = series.Notes,
            Type = series.Type,
            Priority = series.Priority,
            Recurrence = series.Recurrence,
            DueDate = nextDue,
            ScheduledStartAt = nextScheduledStart,
            ScheduledEndAt = nextScheduledEnd,
            QuestSeries = series,
            OverridesQuestSeries = false,
            SeriesOccurrenceDate = nextDue,
            ProjectsQuestSeries = true,
            Tags = new List<string>(series.Tags ?? new List<string>()),
            RewardXp = series.RewardXp,
            Completed = false,
            CreatedAt = now,
            UpdatedAt = now,
            SortOrder = nextSort + 1,
            MyGameId = series.MyGameId,
            SkillId = series.SkillId,
            QuestFolderId = series.QuestFolderId
        };

        series.DueDate = nextDue;
        series.ScheduledStartAt = nextScheduledStart;
        series.ScheduledEndAt = nextScheduledEnd;
        series.UpdatedAt = now;

        await dbContext.Quests.AddAsync(clone);
        return clone;
    }

    private static async Task<QuestSeries> EnsureQuestSeriesAsync(
        LuminaPathDbContext dbContext,
        string userId,
        Quest source,
        DateTime now)
    {
        if (source.QuestSeries != null)
        {
            source.SeriesOccurrenceDate ??= source.DueDate ?? ScheduleDueDate(source.ScheduledStartAt);
            source.ProjectsQuestSeries = true;
            return source.QuestSeries;
        }

        if (source.QuestSeriesId.HasValue)
        {
            var existing = await dbContext.QuestSeries
                .FirstOrDefaultAsync(series => series.Id == source.QuestSeriesId.Value && series.LuminaUserId == userId);
            if (existing != null)
            {
                source.QuestSeries = existing;
                source.SeriesOccurrenceDate ??= source.DueDate ?? ScheduleDueDate(source.ScheduledStartAt);
                source.ProjectsQuestSeries = true;
                return existing;
            }
        }

        var series = CreateSeriesFromQuest(source, now);
        await dbContext.QuestSeries.AddAsync(series);
        source.QuestSeries = series;
        source.OverridesQuestSeries = false;
        source.SeriesOccurrenceDate = source.DueDate ?? ScheduleDueDate(source.ScheduledStartAt);
        source.ProjectsQuestSeries = true;
        return series;
    }

    private static QuestSeries CreateSeriesFromQuest(Quest source, DateTime now)
    {
        return new QuestSeries
        {
            LuminaUserId = source.LuminaUserId,
            Title = source.Title,
            Notes = source.Notes,
            Type = source.Type,
            Priority = source.Priority,
            Recurrence = source.Recurrence,
            DueDate = source.DueDate,
            ScheduledStartAt = source.ScheduledStartAt,
            ScheduledEndAt = source.ScheduledEndAt,
            Tags = new List<string>(source.Tags ?? new List<string>()),
            RewardXp = source.RewardXp,
            CreatedAt = now,
            UpdatedAt = now,
            MyGameId = source.MyGameId,
            SkillId = source.SkillId,
            QuestFolderId = source.QuestFolderId
        };
    }

    private static void ApplySeriesToQuest(Quest quest, QuestSeries series)
    {
        quest.Title = series.Title;
        quest.Notes = series.Notes;
        quest.Type = series.Type;
        quest.Priority = series.Priority;
        quest.Recurrence = series.Recurrence;
        quest.DueDate = series.DueDate;
        quest.ScheduledStartAt = series.ScheduledStartAt;
        quest.ScheduledEndAt = series.ScheduledEndAt;
        quest.Tags = new List<string>(series.Tags ?? new List<string>());
        quest.RewardXp = series.RewardXp;
        quest.MyGameId = series.MyGameId;
        quest.SkillId = series.SkillId;
        quest.QuestFolderId = series.QuestFolderId;
        quest.QuestSeries = series;
        quest.OverridesQuestSeries = false;
        quest.SeriesOccurrenceDate = series.DueDate ?? ScheduleDueDate(series.ScheduledStartAt);
    }

    private static bool TryResolveOccurrenceSchedule(
        QuestSeries series,
        DateTime occurrenceDate,
        QuestOccurrenceCreateDto dto,
        out DateTime? scheduledStartAt,
        out DateTime? scheduledEndAt,
        out string error)
    {
        scheduledStartAt = null;
        scheduledEndAt = null;
        error = string.Empty;

        if (dto.ScheduledStartAt.HasValue || dto.ScheduledEndAt.HasValue)
        {
            return TryBuildSchedule(dto.ScheduledStartAt, dto.ScheduledEndAt, out scheduledStartAt, out scheduledEndAt, out error);
        }

        if (!series.ScheduledStartAt.HasValue || !series.ScheduledEndAt.HasValue)
        {
            error = "Recurring quest needs a schedule before an occurrence can be edited";
            return false;
        }

        var sourceStart = NormalizeDate(series.ScheduledStartAt)!.Value;
        var sourceEnd = NormalizeDate(series.ScheduledEndAt)!.Value;
        var duration = sourceEnd - sourceStart;
        if (duration <= TimeSpan.Zero)
        {
            error = "Recurring quest schedule is invalid";
            return false;
        }

        scheduledStartAt = DateTime.SpecifyKind(occurrenceDate.Date + sourceStart.TimeOfDay, DateTimeKind.Utc);
        scheduledEndAt = scheduledStartAt.Value.Add(duration);
        return true;
    }

    private static DateTime ShiftForRecurrence(DateTime value, QuestRecurrence recurrence)
    {
        return recurrence switch
        {
            QuestRecurrence.Daily => value.AddDays(1),
            QuestRecurrence.Weekly => value.AddDays(7),
            QuestRecurrence.Monthly => value.AddMonths(1),
            _ => value
        };
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

    private static DateTime NormalizeDateOnly(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static DateTime? ScheduleDueDate(DateTime? scheduledStartAt)
    {
        if (!scheduledStartAt.HasValue)
        {
            return null;
        }

        var start = NormalizeDate(scheduledStartAt)!.Value;
        return DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
    }

    private static bool TryBuildSchedule(
        DateTime? scheduledStartAt,
        DateTime? scheduledEndAt,
        out DateTime? normalizedStart,
        out DateTime? normalizedEnd,
        out string error)
    {
        normalizedStart = NormalizeDate(scheduledStartAt);
        normalizedEnd = NormalizeDate(scheduledEndAt);
        error = string.Empty;

        if (!normalizedStart.HasValue && !normalizedEnd.HasValue)
        {
            return true;
        }

        if (!normalizedStart.HasValue || !normalizedEnd.HasValue)
        {
            error = "Scheduled start and end are required together";
            return false;
        }

        if (normalizedEnd.Value <= normalizedStart.Value)
        {
            error = "Scheduled end must be after scheduled start";
            return false;
        }

        return true;
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
            ScheduledStartAt = quest.ScheduledStartAt,
            ScheduledEndAt = quest.ScheduledEndAt,
            QuestSeriesId = quest.QuestSeriesId,
            OverridesQuestSeries = quest.OverridesQuestSeries,
            SeriesOccurrenceDate = quest.SeriesOccurrenceDate,
            ProjectsQuestSeries = quest.ProjectsQuestSeries,
            SeriesRecurrence = quest.QuestSeries?.Recurrence,
            SeriesScheduledStartAt = quest.QuestSeries?.ScheduledStartAt,
            SeriesScheduledEndAt = quest.QuestSeries?.ScheduledEndAt,
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
