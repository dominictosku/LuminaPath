using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public partial class QuestService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly Func<DateTime> _utcNow;

        public QuestService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
            : this(dbContextFactory, () => DateTime.UtcNow)
        {
        }

        public QuestService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, Func<DateTime> utcNow)
        {
            _dbContextFactory = dbContextFactory;
            _utcNow = utcNow;
        }

        private DateTime UtcNow => _utcNow();

        public async Task<QuestBoardDto> GetBoardAsync(string userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var profile = await dbContext.QuestProfiles
                .AsNoTracking()
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(profile => profile.LuminaUserId == userId);

            var quests = await LoadQuestDtos(dbContext, userId);
            var skills = await LoadSkillDtos(dbContext, userId);
            var folders = await LoadFolderDtos(dbContext, userId);

            return new QuestBoardDto
            {
                Xp = profile?.TotalXp ?? 0,
                CurrentStreakDays = profile?.CurrentStreakDays ?? 0,
                LongestStreakDays = profile?.LongestStreakDays ?? 0,
                LastCompletionDate = profile?.LastCompletionDate?.ToDateTime(TimeOnly.MinValue),
                Quests = quests,
                Skills = skills,
                Folders = folders,
                Achievements = profile?.Achievements
                    .OrderByDescending(a => a.UnlockedAt)
                    .Select(ProjectAchievementDto)
                    .ToList() ?? []
            };
        }

        public async Task<List<QuestDto>> GetQuestsForMyGameAsync(string userId, int myGameId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            if (await ResolveOwnedMyGameIdAsync(dbContext, userId, myGameId) is null)
            {
                return [];
            }

            var quests = await QueryQuestDetails(dbContext, userId)
                .Where(quest => quest.MyGameId == myGameId)
                .OrderBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .ToListAsync();

            return quests.Select(ProjectQuestDto).ToList();
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> CreateAsync(string userId, QuestCreateDto dto)
        {
            var title = (dto.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return new FailedResult("Title is required");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var myGameId = await ResolveOwnedMyGameIdAsync(dbContext, userId, dto.MyGameId);
            var skillId = await ResolveOwnedSkillIdAsync(dbContext, userId, dto.SkillId);
            var folderId = await ResolveOwnedFolderIdAsync(dbContext, userId, dto.QuestFolderId);
            if (!TryBuildSchedule(dto.ScheduledStartAt, dto.ScheduledEndAt, out var scheduledStartAt, out var scheduledEndAt, out var scheduleError))
            {
                return new FailedResult(scheduleError);
            }

            var nextSort = await dbContext.Quests
                .Where(q => q.LuminaUserId == userId && q.Type == dto.Type)
                .Select(q => (int?)q.SortOrder)
                .MaxAsync() ?? -1;
            var now = UtcNow;

            var quest = new Quest
            {
                LuminaUserId = userId,
                Title = title,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Type = dto.Type,
                Priority = dto.Priority,
                Recurrence = dto.Recurrence,
                DueDate = ScheduleDueDate(scheduledStartAt) ?? NormalizeDate(dto.DueDate),
                ScheduledStartAt = scheduledStartAt,
                ScheduledEndAt = scheduledEndAt,
                Tags = NormalizeTags(dto.Tags),
                RewardXp = RewardFor(dto.Type),
                Completed = false,
                CreatedAt = now,
                UpdatedAt = now,
                SortOrder = nextSort + 1,
                MyGameId = myGameId,
                SkillId = skillId,
                QuestFolderId = folderId,
                ProjectsQuestSeries = true
            };

            if (quest.Recurrence != QuestRecurrence.None)
            {
                quest.SeriesOccurrenceDate = quest.DueDate ?? ScheduleDueDate(quest.ScheduledStartAt);
                quest.QuestSeries = CreateSeriesFromQuest(quest, now);
            }

            await dbContext.Quests.AddAsync(quest);
            await dbContext.SaveChangesAsync();

            return await BuildMutationResultAsync(dbContext, userId, quest.Id);
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> MaterializeOccurrenceAsync(
            string userId,
            int id,
            QuestOccurrenceCreateDto dto)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var source = await dbContext.Quests
                .Include(q => q.QuestSeries)
                .FirstOrDefaultAsync(q => q.Id == id && q.LuminaUserId == userId);

            if (source == null)
            {
                return new FailedResult("Quest not found");
            }

            if (source.QuestSeriesId is null && source.QuestSeries is null && source.Recurrence == QuestRecurrence.None)
            {
                return new FailedResult("Quest is not recurring");
            }

            var now = UtcNow;
            var series = await EnsureQuestSeriesAsync(dbContext, userId, source, now);
            if (series.Recurrence == QuestRecurrence.None)
            {
                return new FailedResult("Quest is not recurring");
            }

            if (series.Id == 0)
            {
                await dbContext.SaveChangesAsync();
            }

            var occurrenceDate = NormalizeDateOnly(dto.OccurrenceDate);

            if (!TryResolveOccurrenceSchedule(series, occurrenceDate, dto, out var scheduledStartAt, out var scheduledEndAt, out var scheduleError))
            {
                return new FailedResult(scheduleError);
            }

            var occurrenceDay = occurrenceDate;
            var nextDay = occurrenceDay.AddDays(1);
            var existing = await dbContext.Quests
                .Include(q => q.QuestSeries)
                .FirstOrDefaultAsync(q =>
                    q.LuminaUserId == userId
                    && q.QuestSeriesId == series.Id
                    && q.SeriesOccurrenceDate.HasValue
                    && q.SeriesOccurrenceDate.Value >= occurrenceDay
                    && q.SeriesOccurrenceDate.Value < nextDay);

            if (existing != null)
            {
                existing.ScheduledStartAt = scheduledStartAt;
                existing.ScheduledEndAt = scheduledEndAt;
                existing.DueDate = ScheduleDueDate(scheduledStartAt) ?? occurrenceDay;
                existing.SeriesOccurrenceDate = occurrenceDay;
                existing.OverridesQuestSeries = true;
                existing.ProjectsQuestSeries = false;
                existing.UpdatedAt = now;
                await dbContext.SaveChangesAsync();
                return await BuildMutationResultAsync(dbContext, userId, existing.Id);
            }

            var nextSort = await dbContext.Quests
                .Where(q => q.LuminaUserId == userId && q.Type == series.Type)
                .Select(q => (int?)q.SortOrder)
                .MaxAsync() ?? -1;

            var occurrence = new Quest
            {
                LuminaUserId = userId,
                Title = series.Title,
                Notes = series.Notes,
                Type = series.Type,
                Priority = series.Priority,
                Recurrence = series.Recurrence,
                DueDate = ScheduleDueDate(scheduledStartAt) ?? occurrenceDay,
                ScheduledStartAt = scheduledStartAt,
                ScheduledEndAt = scheduledEndAt,
                QuestSeries = series,
                OverridesQuestSeries = true,
                SeriesOccurrenceDate = occurrenceDay,
                ProjectsQuestSeries = false,
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

            await dbContext.Quests.AddAsync(occurrence);
            await dbContext.SaveChangesAsync();
            return await BuildMutationResultAsync(dbContext, userId, occurrence.Id);
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> UpdateAsync(string userId, int id, QuestUpdateDto dto)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var quest = await dbContext.Quests
                .Include(q => q.QuestSeries)
                .FirstOrDefaultAsync(q => q.Id == id && q.LuminaUserId == userId);

            if (quest == null)
            {
                return new FailedResult("Quest not found");
            }

            var now = UtcNow;
            if (ResolveEditScope(quest, dto) == QuestEditScope.Series)
            {
                return await UpdateSeriesAsync(dbContext, userId, quest, dto, now);
            }

            var profile = await GetOrCreateProfileAsync(dbContext, userId, now);
            var overridesSeries = false;

            if (dto.Title is not null)
            {
                var trimmed = dto.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    return new FailedResult("Title cannot be empty");
                }
                quest.Title = trimmed;
                overridesSeries = true;
            }

            if (dto.Notes is not null)
            {
                quest.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
                overridesSeries = true;
            }

            if (dto.Type.HasValue && dto.Type.Value != quest.Type)
            {
                quest.Type = dto.Type.Value;
                if (!quest.Completed)
                {
                    quest.RewardXp = RewardFor(quest.Type);
                }
                overridesSeries = true;
            }

            if (dto.Priority.HasValue)
            {
                quest.Priority = dto.Priority.Value;
                overridesSeries = true;
            }

            if (dto.Recurrence.HasValue)
            {
                quest.Recurrence = dto.Recurrence.Value;
                overridesSeries = true;
            }

            if (dto.ClearDueDate == true)
            {
                quest.DueDate = null;
                overridesSeries = true;
            }
            else if (dto.DueDate.HasValue)
            {
                quest.DueDate = NormalizeDate(dto.DueDate);
                overridesSeries = true;
            }

            if (dto.ClearSchedule == true)
            {
                quest.ScheduledStartAt = null;
                quest.ScheduledEndAt = null;
                overridesSeries = true;
            }
            else if (dto.ScheduledStartAt.HasValue || dto.ScheduledEndAt.HasValue)
            {
                var nextStart = dto.ScheduledStartAt ?? quest.ScheduledStartAt;
                var nextEnd = dto.ScheduledEndAt ?? quest.ScheduledEndAt;
                if (!TryBuildSchedule(nextStart, nextEnd, out var scheduledStartAt, out var scheduledEndAt, out var scheduleError))
                {
                    return new FailedResult(scheduleError);
                }

                quest.ScheduledStartAt = scheduledStartAt;
                quest.ScheduledEndAt = scheduledEndAt;
                quest.DueDate = ScheduleDueDate(scheduledStartAt);
                overridesSeries = true;
            }

            if (dto.Tags is not null)
            {
                quest.Tags = NormalizeTags(dto.Tags);
                overridesSeries = true;
            }

            if (dto.ClearMyGame == true)
            {
                quest.MyGameId = null;
                overridesSeries = true;
            }
            else if (dto.MyGameId.HasValue)
            {
                quest.MyGameId = await ResolveOwnedMyGameIdAsync(dbContext, userId, dto.MyGameId) ?? quest.MyGameId;
                overridesSeries = true;
            }

            if (dto.ClearSkill == true)
            {
                quest.SkillId = null;
                overridesSeries = true;
            }
            else if (dto.SkillId.HasValue)
            {
                quest.SkillId = await ResolveOwnedSkillIdAsync(dbContext, userId, dto.SkillId) ?? quest.SkillId;
                overridesSeries = true;
            }

            if (dto.ClearQuestFolder == true)
            {
                quest.QuestFolderId = null;
                overridesSeries = true;
            }
            else if (dto.QuestFolderId.HasValue)
            {
                quest.QuestFolderId = await ResolveOwnedFolderIdAsync(dbContext, userId, dto.QuestFolderId) ?? quest.QuestFolderId;
                overridesSeries = true;
            }

            if (dto.SortOrder.HasValue)
            {
                quest.SortOrder = dto.SortOrder.Value;
            }

            if (quest.QuestSeriesId.HasValue && overridesSeries)
            {
                quest.OverridesQuestSeries = true;
            }

            Quest? spawned = null;
            int? awardedSkillXp = null;
            int? awardedSkillId = null;
            var unlockedAchievements = new List<Achievement>();

            if (dto.Completed.HasValue && dto.Completed.Value != quest.Completed)
            {
                if (dto.Completed.Value)
                {
                    quest.Completed = true;
                    quest.CompletedAt = now;
                    profile.TotalXp = Math.Max(0, profile.TotalXp + quest.RewardXp);

                    if (quest.SkillId.HasValue)
                    {
                        var skill = await dbContext.QuestSkills
                            .FirstOrDefaultAsync(s => s.Id == quest.SkillId.Value && s.LuminaUserId == userId);
                        if (skill != null)
                        {
                            var skillReward = SkillRewardFor(quest.Type);
                            skill.Xp = Math.Max(0, skill.Xp + skillReward);
                            awardedSkillXp = skillReward;
                            awardedSkillId = skill.Id;
                        }
                    }

                    UpdateStreakOnCompletion(profile, now);

                    if (quest.ProjectsQuestSeries && (quest.QuestSeriesId.HasValue || quest.Recurrence != QuestRecurrence.None))
                    {
                        spawned = await SpawnNextRecurrenceAsync(dbContext, userId, quest, now);
                    }

                    unlockedAchievements = await EvaluateAchievementsAsync(dbContext, userId, profile, quest, now);
                }
                else
                {
                    quest.Completed = false;
                    quest.CompletedAt = null;
                    profile.TotalXp = Math.Max(0, profile.TotalXp - quest.RewardXp);

                    if (quest.SkillId.HasValue)
                    {
                        var skill = await dbContext.QuestSkills
                            .FirstOrDefaultAsync(s => s.Id == quest.SkillId.Value && s.LuminaUserId == userId);
                        if (skill != null)
                        {
                            skill.Xp = Math.Max(0, skill.Xp - SkillRewardFor(quest.Type));
                        }
                    }
                }
            }

            quest.UpdatedAt = now;
            profile.UpdatedAt = now;

            await dbContext.SaveChangesAsync();
            var result = await BuildMutationResultAsync(dbContext, userId, quest.Id, spawned?.Id);
            result.AwardedSkillXp = awardedSkillXp;
            result.AwardedSkillId = awardedSkillId;
            result.UnlockedAchievements = unlockedAchievements.Select(ProjectAchievementDto).ToList();
            return result;
        }

        private async Task<Result<QuestMutationResultDto, FailedResult>> UpdateSeriesAsync(
            LuminaPathDbContext dbContext,
            string userId,
            Quest quest,
            QuestUpdateDto dto,
            DateTime now)
        {
            var series = await EnsureQuestSeriesAsync(dbContext, userId, quest, now);

            if (dto.Title is not null)
            {
                var trimmed = dto.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    return new FailedResult("Title cannot be empty");
                }
                series.Title = trimmed;
            }

            if (dto.Notes is not null)
            {
                series.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            }

            if (dto.Type.HasValue)
            {
                series.Type = dto.Type.Value;
                series.RewardXp = RewardFor(series.Type);
            }

            if (dto.Priority.HasValue)
            {
                series.Priority = dto.Priority.Value;
            }

            if (dto.Recurrence.HasValue)
            {
                series.Recurrence = dto.Recurrence.Value;
            }

            if (dto.ClearDueDate == true)
            {
                series.DueDate = null;
            }
            else if (dto.DueDate.HasValue)
            {
                series.DueDate = NormalizeDate(dto.DueDate);
            }

            if (dto.ClearSchedule == true)
            {
                series.ScheduledStartAt = null;
                series.ScheduledEndAt = null;
            }
            else if (dto.ScheduledStartAt.HasValue || dto.ScheduledEndAt.HasValue)
            {
                var nextStart = dto.ScheduledStartAt ?? series.ScheduledStartAt ?? quest.ScheduledStartAt;
                var nextEnd = dto.ScheduledEndAt ?? series.ScheduledEndAt ?? quest.ScheduledEndAt;
                if (!TryBuildSchedule(nextStart, nextEnd, out var scheduledStartAt, out var scheduledEndAt, out var scheduleError))
                {
                    return new FailedResult(scheduleError);
                }

                series.ScheduledStartAt = scheduledStartAt;
                series.ScheduledEndAt = scheduledEndAt;
                series.DueDate = ScheduleDueDate(scheduledStartAt);
            }

            if (dto.Tags is not null)
            {
                series.Tags = NormalizeTags(dto.Tags);
            }

            if (dto.ClearMyGame == true)
            {
                series.MyGameId = null;
            }
            else if (dto.MyGameId.HasValue)
            {
                series.MyGameId = await ResolveOwnedMyGameIdAsync(dbContext, userId, dto.MyGameId) ?? series.MyGameId;
            }

            if (dto.ClearSkill == true)
            {
                series.SkillId = null;
            }
            else if (dto.SkillId.HasValue)
            {
                series.SkillId = await ResolveOwnedSkillIdAsync(dbContext, userId, dto.SkillId) ?? series.SkillId;
            }

            if (dto.ClearQuestFolder == true)
            {
                series.QuestFolderId = null;
            }
            else if (dto.QuestFolderId.HasValue)
            {
                series.QuestFolderId = await ResolveOwnedFolderIdAsync(dbContext, userId, dto.QuestFolderId) ?? series.QuestFolderId;
            }

            series.UpdatedAt = now;
            ApplySeriesToQuest(quest, series);
            quest.UpdatedAt = now;

            await dbContext.SaveChangesAsync();
            return await BuildMutationResultAsync(dbContext, userId, quest.Id);
        }

        private static QuestEditScope ResolveEditScope(Quest quest, QuestUpdateDto dto)
        {
            if (dto.EditScope.HasValue)
            {
                return dto.EditScope.Value;
            }

            var isRecurringQuest = quest.QuestSeriesId.HasValue
                || quest.QuestSeries != null
                || quest.Recurrence != QuestRecurrence.None;
            return isRecurringQuest && UpdatesSeriesTemplateFields(dto)
                ? QuestEditScope.Series
                : QuestEditScope.Occurrence;
        }

        private static bool UpdatesSeriesTemplateFields(QuestUpdateDto dto)
        {
            return dto.Title is not null
                || dto.Notes is not null
                || dto.Type.HasValue
                || dto.Priority.HasValue
                || dto.Recurrence.HasValue
                || dto.DueDate.HasValue
                || dto.ClearDueDate == true
                || dto.ScheduledStartAt.HasValue
                || dto.ScheduledEndAt.HasValue
                || dto.ClearSchedule == true
                || dto.Tags is not null
                || dto.MyGameId.HasValue
                || dto.ClearMyGame == true
                || dto.SkillId.HasValue
                || dto.ClearSkill == true
                || dto.QuestFolderId.HasValue
                || dto.ClearQuestFolder == true;
        }

        public async Task<Result<int, FailedResult>> DeleteAsync(string userId, int id)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var quest = await dbContext.Quests
                .FirstOrDefaultAsync(q => q.Id == id && q.LuminaUserId == userId);

            if (quest == null)
            {
                return new FailedResult("Quest not found");
            }

            dbContext.Quests.Remove(quest);
            await dbContext.SaveChangesAsync();
            return id;
        }

        public async Task<Result<int, FailedResult>> ReorderAsync(string userId, List<QuestReorderItemDto> items)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var ids = items.Select(item => item.Id).Distinct().ToList();
            var quests = await dbContext.Quests
                .Where(q => q.LuminaUserId == userId && ids.Contains(q.Id))
                .ToListAsync();
            var questsById = quests.ToDictionary(q => q.Id);

            foreach (var item in items)
            {
                if (!questsById.TryGetValue(item.Id, out var quest))
                {
                    continue;
                }
                quest.SortOrder = item.SortOrder;
                quest.Type = item.Type;
                quest.UpdatedAt = UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return items.Count;
        }

        public async Task<QuestBoardDto> SaveSkillsAsync(string userId, QuestBoardDto board)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var now = UtcNow;
            var profile = await GetOrCreateProfileAsync(dbContext, userId, now);
            profile.TotalXp = Math.Max(0, board.Xp);
            profile.UpdatedAt = now;

            await UpsertSkillsAsync(dbContext, userId, board.Skills, now);
            await dbContext.SaveChangesAsync();

            return await GetBoardAsync(userId);
        }

    }

}
