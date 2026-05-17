using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class QuestService
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

            return new QuestBoardDto
            {
                Xp = profile?.TotalXp ?? 0,
                CurrentStreakDays = profile?.CurrentStreakDays ?? 0,
                LongestStreakDays = profile?.LongestStreakDays ?? 0,
                LastCompletionDate = profile?.LastCompletionDate?.ToDateTime(TimeOnly.MinValue),
                Quests = quests,
                Skills = skills,
                Achievements = profile?.Achievements
                    .OrderByDescending(a => a.UnlockedAt)
                    .Select(ProjectAchievementDto)
                    .ToList() ?? []
            };
        }

        public async Task<List<QuestDto>> GetQuestsForMyGameAsync(string userId, int myGameId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var ownsGame = await dbContext.MyGames
                .AsNoTracking()
                .AnyAsync(myGame => myGame.Id == myGameId && myGame.LuminaUserId == userId);

            if (!ownsGame)
            {
                return [];
            }

            return await dbContext.Quests
                .AsNoTracking()
                .Include(quest => quest.MyGame).ThenInclude(myGame => myGame!.Game)
                .Include(quest => quest.Skill)
                .Include(quest => quest.Subtasks)
                .Where(quest => quest.LuminaUserId == userId && quest.MyGameId == myGameId)
                .OrderBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .Select(quest => ProjectQuestDto(quest))
                .ToListAsync();
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> CreateAsync(string userId, QuestCreateDto dto)
        {
            var title = (dto.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return new FailedResult("Title is required");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            int? myGameId = null;
            if (dto.MyGameId.HasValue)
            {
                var ownsGame = await dbContext.MyGames
                    .AsNoTracking()
                    .AnyAsync(myGame => myGame.Id == dto.MyGameId.Value && myGame.LuminaUserId == userId);
                if (ownsGame)
                {
                    myGameId = dto.MyGameId.Value;
                }
            }

            int? skillId = null;
            if (dto.SkillId.HasValue)
            {
                var ownsSkill = await dbContext.QuestSkills
                    .AsNoTracking()
                    .AnyAsync(skill => skill.Id == dto.SkillId.Value && skill.LuminaUserId == userId);
                if (ownsSkill)
                {
                    skillId = dto.SkillId.Value;
                }
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
                DueDate = NormalizeDate(dto.DueDate),
                Tags = NormalizeTags(dto.Tags),
                RewardXp = RewardFor(dto.Type),
                Completed = false,
                CreatedAt = now,
                UpdatedAt = now,
                SortOrder = nextSort + 1,
                MyGameId = myGameId,
                SkillId = skillId
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
                var ownsGame = await dbContext.MyGames
                    .AsNoTracking()
                    .AnyAsync(myGame => myGame.Id == dto.MyGameId.Value && myGame.LuminaUserId == userId);
                if (ownsGame)
                {
                    quest.MyGameId = dto.MyGameId.Value;
                }
            }

            if (dto.ClearSkill == true)
            {
                quest.SkillId = null;
            }
            else if (dto.SkillId.HasValue)
            {
                var ownsSkill = await dbContext.QuestSkills
                    .AsNoTracking()
                    .AnyAsync(skill => skill.Id == dto.SkillId.Value && skill.LuminaUserId == userId);
                if (ownsSkill)
                {
                    quest.SkillId = dto.SkillId.Value;
                }
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

        private static async Task<QuestMutationResultDto> BuildMutationResultAsync(LuminaPathDbContext dbContext, string userId, int questId, int? spawnedId = null)
        {
            var quest = await dbContext.Quests
                .AsNoTracking()
                .Include(q => q.MyGame).ThenInclude(g => g!.Game)
                .Include(q => q.Skill)
                .Include(q => q.Subtasks)
                .Where(q => q.Id == questId && q.LuminaUserId == userId)
                .FirstAsync();

            QuestDto? spawned = null;
            if (spawnedId is int sid)
            {
                var spawnedEntity = await dbContext.Quests
                    .AsNoTracking()
                    .Include(q => q.MyGame).ThenInclude(g => g!.Game)
                    .Include(q => q.Skill)
                    .Include(q => q.Subtasks)
                    .Where(q => q.Id == sid && q.LuminaUserId == userId)
                    .FirstOrDefaultAsync();
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
            var quests = await dbContext.Quests
                .AsNoTracking()
                .Include(q => q.MyGame).ThenInclude(g => g!.Game)
                .Include(q => q.Skill)
                .Include(q => q.Subtasks)
                .Where(quest => quest.LuminaUserId == userId)
                .OrderBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .ToListAsync();

            return quests.Select(ProjectQuestDto).ToList();
        }

        private static async Task<List<QuestSkillDto>> LoadSkillDtos(LuminaPathDbContext dbContext, string userId)
        {
            var skills = await dbContext.QuestSkills
                .AsNoTracking()
                .Include(skill => skill.Nodes)
                .Where(skill => skill.LuminaUserId == userId)
                .OrderBy(skill => skill.SortOrder)
                .ThenBy(skill => skill.Id)
                .ToListAsync();

            return skills.Select(skill => new QuestSkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Icon = skill.Icon,
                Color = skill.Color,
                Xp = skill.Xp,
                SortOrder = skill.SortOrder,
                Nodes = skill.Nodes
                    .OrderBy(node => node.SortOrder)
                    .ThenBy(node => node.Id)
                    .Select(node => new QuestSkillNodeDto
                    {
                        Id = node.Id,
                        Name = node.Name,
                        Unlocked = node.Unlocked,
                        UnlockedAt = node.UnlockedAt,
                        SortOrder = node.SortOrder
                    })
                    .ToList()
            }).ToList();
        }

        private static async Task UpsertSkillsAsync(
            LuminaPathDbContext dbContext,
            string userId,
            List<QuestSkillDto> incoming,
            DateTime now)
        {
            var existing = await dbContext.QuestSkills
                .Include(skill => skill.Nodes)
                .Where(skill => skill.LuminaUserId == userId)
                .ToListAsync();
            var existingById = existing.ToDictionary(skill => skill.Id);

            var keepSkillIds = new HashSet<int>();
            foreach (var (dto, skillIndex) in incoming.Select((dto, index) => (dto, index)))
            {
                var name = (dto.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (dto.Id > 0 && existingById.TryGetValue(dto.Id, out var existingSkill))
                {
                    ApplySkill(existingSkill, dto, skillIndex, name);
                    UpsertNodes(dbContext, existingSkill, dto.Nodes, now);
                    keepSkillIds.Add(existingSkill.Id);
                }
                else
                {
                    var newSkill = new QuestSkill
                    {
                        LuminaUserId = userId,
                        Nodes = []
                    };
                    ApplySkill(newSkill, dto, skillIndex, name);
                    foreach (var (nodeDto, nodeIndex) in dto.Nodes.Select((node, index) => (node, index)))
                    {
                        var nodeName = (nodeDto.Name ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(nodeName))
                        {
                            continue;
                        }
                        newSkill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName, now));
                    }
                    await dbContext.QuestSkills.AddAsync(newSkill);
                }
            }

            var skillsToDelete = existing.Where(skill => !keepSkillIds.Contains(skill.Id)).ToList();
            if (skillsToDelete.Count > 0)
            {
                dbContext.QuestSkills.RemoveRange(skillsToDelete);
            }
        }

        private static void ApplySkill(QuestSkill skill, QuestSkillDto dto, int fallbackSortOrder, string name)
        {
            skill.Name = name;
            skill.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "code-slash-outline" : dto.Icon;
            skill.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#2563eb" : dto.Color;
            skill.Xp = Math.Max(0, dto.Xp);
            skill.SortOrder = dto.SortOrder == 0 ? fallbackSortOrder : dto.SortOrder;
        }

        private static void UpsertNodes(LuminaPathDbContext dbContext, QuestSkill skill, List<QuestSkillNodeDto> nodeDtos, DateTime now)
        {
            var nodesById = skill.Nodes.ToDictionary(node => node.Id);
            var keepNodeIds = new HashSet<int>();

            foreach (var (nodeDto, nodeIndex) in nodeDtos.Select((node, index) => (node, index)))
            {
                var nodeName = (nodeDto.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(nodeName))
                {
                    continue;
                }

                if (nodeDto.Id > 0 && nodesById.TryGetValue(nodeDto.Id, out var existingNode))
                {
                    existingNode.Name = nodeName;
                    existingNode.Unlocked = nodeDto.Unlocked;
                    existingNode.UnlockedAt = nodeDto.Unlocked ? nodeDto.UnlockedAt ?? now : null;
                    existingNode.SortOrder = nodeDto.SortOrder == 0 ? nodeIndex : nodeDto.SortOrder;
                    keepNodeIds.Add(existingNode.Id);
                }
                else
                {
                    skill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName, now));
                }
            }

            var nodesToDelete = skill.Nodes
                .Where(node => node.Id > 0 && !keepNodeIds.Contains(node.Id))
                .ToList();
            if (nodesToDelete.Count > 0)
            {
                dbContext.QuestSkillNodes.RemoveRange(nodesToDelete);
                foreach (var node in nodesToDelete)
                {
                    skill.Nodes.Remove(node);
                }
            }
        }

        private static QuestSkillNode BuildNewNode(QuestSkillNodeDto dto, int fallbackSortOrder, string name, DateTime now)
        {
            return new QuestSkillNode
            {
                Name = name,
                Unlocked = dto.Unlocked,
                UnlockedAt = dto.Unlocked ? dto.UnlockedAt ?? now : null,
                SortOrder = dto.SortOrder == 0 ? fallbackSortOrder : dto.SortOrder
            };
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

        // -------- Subtasks --------

        public async Task<Result<QuestMutationResultDto, FailedResult>> AddSubtaskAsync(string userId, int questId, QuestSubtaskCreateDto dto)
        {
            var title = (dto.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return new FailedResult("Subtask title is required");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var quest = await dbContext.Quests
                .Include(q => q.Subtasks)
                .FirstOrDefaultAsync(q => q.Id == questId && q.LuminaUserId == userId);
            if (quest == null)
            {
                return new FailedResult("Quest not found");
            }

            var nextSort = quest.Subtasks.Count == 0 ? 0 : quest.Subtasks.Max(s => s.SortOrder) + 1;
            var now = UtcNow;
            var subtask = new QuestSubtask
            {
                QuestId = quest.Id,
                Title = title,
                SortOrder = nextSort,
                CreatedAt = now
            };
            quest.Subtasks.Add(subtask);
            quest.UpdatedAt = now;
            await dbContext.SaveChangesAsync();

            return await BuildMutationResultAsync(dbContext, userId, quest.Id);
        }

        public async Task<Result<QuestMutationResultDto, FailedResult>> UpdateSubtaskAsync(string userId, int questId, int subtaskId, QuestSubtaskUpdateDto dto)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var quest = await dbContext.Quests
                .Include(q => q.Subtasks)
                .FirstOrDefaultAsync(q => q.Id == questId && q.LuminaUserId == userId);
            if (quest == null)
            {
                return new FailedResult("Quest not found");
            }

            var subtask = quest.Subtasks.FirstOrDefault(s => s.Id == subtaskId);
            if (subtask == null)
            {
                return new FailedResult("Subtask not found");
            }

            if (dto.Title is not null)
            {
                var trimmed = dto.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    return new FailedResult("Subtask title cannot be empty");
                }
                subtask.Title = trimmed;
            }

            if (dto.Completed.HasValue)
            {
                var now = UtcNow;
                if (dto.Completed.Value && !subtask.Completed)
                {
                    subtask.Completed = true;
                    subtask.CompletedAt = now;
                }
                else if (!dto.Completed.Value && subtask.Completed)
                {
                    subtask.Completed = false;
                    subtask.CompletedAt = null;
                }
            }

            if (dto.SortOrder.HasValue)
            {
                subtask.SortOrder = dto.SortOrder.Value;
            }

            quest.UpdatedAt = UtcNow;
            await dbContext.SaveChangesAsync();

            return await BuildMutationResultAsync(dbContext, userId, quest.Id);
        }

        public async Task<Result<int, FailedResult>> DeleteSubtaskAsync(string userId, int questId, int subtaskId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var subtask = await dbContext.QuestSubtasks
                .Include(s => s.Quest)
                .FirstOrDefaultAsync(s => s.Id == subtaskId && s.QuestId == questId && s.Quest!.LuminaUserId == userId);
            if (subtask == null)
            {
                return new FailedResult("Subtask not found");
            }

            dbContext.QuestSubtasks.Remove(subtask);
            await dbContext.SaveChangesAsync();
            return subtaskId;
        }

        // -------- Achievements --------

        private static async Task<List<Achievement>> EvaluateAchievementsAsync(LuminaPathDbContext dbContext, string userId, QuestProfile profile, Quest justCompleted, DateTime now)
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

    internal static class AchievementCatalog
    {
        public const string FirstQuest = "first_quest";
        public const string TenQuests = "ten_quests";
        public const string HundredQuests = "hundred_quests";
        public const string FirstMainQuest = "first_main_quest";
        public const string DailyDiscipline = "daily_discipline";
        public const string Completionist = "completionist";
        public const string SevenDayStreak = "seven_day_streak";
        public const string ThirtyDayStreak = "thirty_day_streak";

        private static readonly Dictionary<string, AchievementDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [FirstQuest] = new("First Steps", "Complete your first quest.", "footsteps-outline"),
            [TenQuests] = new("Apprentice", "Complete 10 quests.", "ribbon-outline"),
            [HundredQuests] = new("Centurion", "Complete 100 quests.", "trophy-outline"),
            [FirstMainQuest] = new("Main Story", "Complete your first main quest.", "map-outline"),
            [DailyDiscipline] = new("Daily Discipline", "Complete a recurring quest.", "refresh-outline"),
            [Completionist] = new("Completionist", "Finish every subtask on a boss quest.", "checkmark-done-outline"),
            [SevenDayStreak] = new("Week One", "Maintain a 7-day quest streak.", "flame-outline"),
            [ThirtyDayStreak] = new("Unbroken", "Maintain a 30-day quest streak.", "flame")
        };

        public static AchievementDefinition? TryGet(string code)
        {
            return Definitions.TryGetValue(code, out var def) ? def : null;
        }
    }

    internal sealed record AchievementDefinition(string Title, string Description, string Icon);
}
