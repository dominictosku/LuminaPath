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
                QuestFolderId = folderId
            };

            await dbContext.Quests.AddAsync(quest);
            await dbContext.SaveChangesAsync();

            return await BuildMutationResultAsync(dbContext, userId, quest.Id);
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> UpdateAsync(string userId, int id, QuestUpdateDto dto)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var quest = await dbContext.Quests
                .FirstOrDefaultAsync(q => q.Id == id && q.LuminaUserId == userId);

            if (quest == null)
            {
                return new FailedResult("Quest not found");
            }

            var now = UtcNow;
            var profile = await GetOrCreateProfileAsync(dbContext, userId, now);

            if (dto.Title is not null)
            {
                var trimmed = dto.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    return new FailedResult("Title cannot be empty");
                }
                quest.Title = trimmed;
            }

            if (dto.Notes is not null)
            {
                quest.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            }

            if (dto.Type.HasValue && dto.Type.Value != quest.Type)
            {
                quest.Type = dto.Type.Value;
                if (!quest.Completed)
                {
                    quest.RewardXp = RewardFor(quest.Type);
                }
            }

            if (dto.Priority.HasValue)
            {
                quest.Priority = dto.Priority.Value;
            }

            if (dto.Recurrence.HasValue)
            {
                quest.Recurrence = dto.Recurrence.Value;
            }

            if (dto.ClearDueDate == true)
            {
                quest.DueDate = null;
            }
            else if (dto.DueDate.HasValue)
            {
                quest.DueDate = NormalizeDate(dto.DueDate);
            }

            if (dto.ClearSchedule == true)
            {
                quest.ScheduledStartAt = null;
                quest.ScheduledEndAt = null;
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
            }

            if (dto.Tags is not null)
            {
                quest.Tags = NormalizeTags(dto.Tags);
            }

            if (dto.ClearMyGame == true)
            {
                quest.MyGameId = null;
            }
            else if (dto.MyGameId.HasValue)
            {
                quest.MyGameId = await ResolveOwnedMyGameIdAsync(dbContext, userId, dto.MyGameId) ?? quest.MyGameId;
            }

            if (dto.ClearSkill == true)
            {
                quest.SkillId = null;
            }
            else if (dto.SkillId.HasValue)
            {
                quest.SkillId = await ResolveOwnedSkillIdAsync(dbContext, userId, dto.SkillId) ?? quest.SkillId;
            }

            if (dto.ClearQuestFolder == true)
            {
                quest.QuestFolderId = null;
            }
            else if (dto.QuestFolderId.HasValue)
            {
                quest.QuestFolderId = await ResolveOwnedFolderIdAsync(dbContext, userId, dto.QuestFolderId) ?? quest.QuestFolderId;
            }

            if (dto.SortOrder.HasValue)
            {
                quest.SortOrder = dto.SortOrder.Value;
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

                    if (quest.Recurrence != QuestRecurrence.None)
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
