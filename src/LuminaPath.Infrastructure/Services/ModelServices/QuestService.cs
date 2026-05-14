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

        public QuestService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<QuestBoardDto> GetBoardAsync(string userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var profile = await dbContext.QuestProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.LuminaUserId == userId);

            var quests = await LoadQuestDtos(dbContext, userId);
            var skills = await LoadSkillDtos(dbContext, userId);

            return new QuestBoardDto
            {
                Xp = profile?.TotalXp ?? 0,
                Quests = quests,
                Skills = skills
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

            var nextSort = await dbContext.Quests
                .Where(q => q.LuminaUserId == userId && q.Type == dto.Type)
                .Select(q => (int?)q.SortOrder)
                .MaxAsync() ?? -1;

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SortOrder = nextSort + 1,
                MyGameId = myGameId
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

            var profile = await GetOrCreateProfileAsync(dbContext, userId);

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

            if (dto.SortOrder.HasValue)
            {
                quest.SortOrder = dto.SortOrder.Value;
            }

            Quest? spawned = null;
            if (dto.Completed.HasValue && dto.Completed.Value != quest.Completed)
            {
                if (dto.Completed.Value)
                {
                    quest.Completed = true;
                    quest.CompletedAt = DateTime.UtcNow;
                    profile.TotalXp = Math.Max(0, profile.TotalXp + quest.RewardXp);

                    if (quest.Recurrence != QuestRecurrence.None)
                    {
                        spawned = await SpawnNextRecurrenceAsync(dbContext, userId, quest);
                    }
                }
                else
                {
                    quest.Completed = false;
                    quest.CompletedAt = null;
                    profile.TotalXp = Math.Max(0, profile.TotalXp - quest.RewardXp);
                }
            }

            quest.UpdatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            return await BuildMutationResultAsync(dbContext, userId, quest.Id, spawned?.Id);
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
                quest.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return items.Count;
        }

        public async Task<QuestBoardDto> SaveSkillsAsync(string userId, QuestBoardDto board)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var profile = await GetOrCreateProfileAsync(dbContext, userId);
            profile.TotalXp = Math.Max(0, board.Xp);
            profile.UpdatedAt = DateTime.UtcNow;

            await UpsertSkillsAsync(dbContext, userId, board.Skills);
            await dbContext.SaveChangesAsync();

            return await GetBoardAsync(userId);
        }

        private static async Task<QuestMutationResultDto> BuildMutationResultAsync(LuminaPathDbContext dbContext, string userId, int questId, int? spawnedId = null)
        {
            var quest = await dbContext.Quests
                .AsNoTracking()
                .Include(q => q.MyGame)
                    .ThenInclude(myGame => myGame!.Game)
                .Where(q => q.Id == questId && q.LuminaUserId == userId)
                .Select(q => ProjectQuestDto(q))
                .FirstAsync();

            QuestDto? spawned = null;
            if (spawnedId is int sid)
            {
                spawned = await dbContext.Quests
                    .AsNoTracking()
                    .Include(q => q.MyGame)
                        .ThenInclude(myGame => myGame!.Game)
                    .Where(q => q.Id == sid && q.LuminaUserId == userId)
                    .Select(q => ProjectQuestDto(q))
                    .FirstOrDefaultAsync();
            }

            var profile = await dbContext.QuestProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LuminaUserId == userId);

            return new QuestMutationResultDto
            {
                Quest = quest,
                SpawnedQuest = spawned,
                TotalXp = profile?.TotalXp ?? 0
            };
        }

        private static async Task<Quest> SpawnNextRecurrenceAsync(LuminaPathDbContext dbContext, string userId, Quest source)
        {
            var anchor = source.DueDate ?? source.CompletedAt ?? DateTime.UtcNow;
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SortOrder = nextSort + 1,
                MyGameId = source.MyGameId
            };

            await dbContext.Quests.AddAsync(clone);
            return clone;
        }

        private static async Task<QuestProfile> GetOrCreateProfileAsync(LuminaPathDbContext dbContext, string userId)
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                GameName = quest.MyGame == null ? null : quest.MyGame.Game!.Name
            };
        }

        private static async Task<List<QuestDto>> LoadQuestDtos(LuminaPathDbContext dbContext, string userId)
        {
            return await dbContext.Quests
                .AsNoTracking()
                .Where(quest => quest.LuminaUserId == userId)
                .OrderBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .Select(quest => ProjectQuestDto(quest))
                .ToListAsync();
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
            List<QuestSkillDto> incoming)
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
                    UpsertNodes(dbContext, existingSkill, dto.Nodes);
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
                        newSkill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName));
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

        private static void UpsertNodes(LuminaPathDbContext dbContext, QuestSkill skill, List<QuestSkillNodeDto> nodeDtos)
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
                    existingNode.UnlockedAt = nodeDto.Unlocked ? nodeDto.UnlockedAt ?? DateTime.UtcNow : null;
                    existingNode.SortOrder = nodeDto.SortOrder == 0 ? nodeIndex : nodeDto.SortOrder;
                    keepNodeIds.Add(existingNode.Id);
                }
                else
                {
                    skill.Nodes.Add(BuildNewNode(nodeDto, nodeIndex, nodeName));
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

        private static QuestSkillNode BuildNewNode(QuestSkillNodeDto dto, int fallbackSortOrder, string name)
        {
            return new QuestSkillNode
            {
                Name = name,
                Unlocked = dto.Unlocked,
                UnlockedAt = dto.Unlocked ? dto.UnlockedAt ?? DateTime.UtcNow : null,
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
    }
}
