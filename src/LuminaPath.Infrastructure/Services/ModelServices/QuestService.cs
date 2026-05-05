using LuminaPath.Core.Dtos;
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

            if (profile == null && quests.Count == 0 && skills.Count == 0)
            {
                return CreateDefaultBoard();
            }

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
                .OrderBy(quest => quest.Type)
                .ThenBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .Select(quest => new QuestDto
                {
                    Id = quest.Id,
                    Title = quest.Title,
                    Type = quest.Type,
                    RewardXp = quest.RewardXp,
                    Completed = quest.Completed,
                    CompletedAt = quest.CompletedAt,
                    CreatedAt = quest.CreatedAt,
                    SortOrder = quest.SortOrder,
                    MyGameId = quest.MyGameId,
                    GameName = quest.MyGame == null ? null : quest.MyGame.Game!.Name
                })
                .ToListAsync();
        }

        public async Task<QuestBoardDto> SaveBoardAsync(string userId, QuestBoardDto board)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            await UpsertProfileAsync(dbContext, userId, board.Xp);

            var ownedMyGameIds = await dbContext.MyGames
                .AsNoTracking()
                .Where(myGame => myGame.LuminaUserId == userId)
                .Select(myGame => myGame.Id)
                .ToListAsync();

            await UpsertQuestsAsync(dbContext, userId, board.Quests, ownedMyGameIds);
            await UpsertSkillsAsync(dbContext, userId, board.Skills);

            await dbContext.SaveChangesAsync();
            return await GetBoardAsync(userId);
        }

        private static async Task<List<QuestDto>> LoadQuestDtos(LuminaPathDbContext dbContext, string userId)
        {
            return await dbContext.Quests
                .AsNoTracking()
                .Where(quest => quest.LuminaUserId == userId)
                .OrderBy(quest => quest.MyGameId == null ? 0 : 1)
                .ThenBy(quest => quest.Type)
                .ThenBy(quest => quest.SortOrder)
                .ThenBy(quest => quest.Id)
                .Select(quest => new QuestDto
                {
                    Id = quest.Id,
                    Title = quest.Title,
                    Type = quest.Type,
                    RewardXp = quest.RewardXp,
                    Completed = quest.Completed,
                    CompletedAt = quest.CompletedAt,
                    CreatedAt = quest.CreatedAt,
                    SortOrder = quest.SortOrder,
                    MyGameId = quest.MyGameId,
                    GameName = quest.MyGame == null ? null : quest.MyGame.Game!.Name
                })
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

        private static async Task UpsertProfileAsync(LuminaPathDbContext dbContext, string userId, int xp)
        {
            var profile = await dbContext.QuestProfiles
                .FirstOrDefaultAsync(profile => profile.LuminaUserId == userId);

            if (profile == null)
            {
                profile = new QuestProfile
                {
                    LuminaUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await dbContext.QuestProfiles.AddAsync(profile);
            }

            profile.TotalXp = Math.Max(0, xp);
            profile.UpdatedAt = DateTime.UtcNow;
        }

        private static async Task UpsertQuestsAsync(
            LuminaPathDbContext dbContext,
            string userId,
            List<QuestDto> incoming,
            List<int> ownedMyGameIds)
        {
            var existing = await dbContext.Quests
                .Where(quest => quest.LuminaUserId == userId)
                .ToListAsync();
            var existingById = existing.ToDictionary(quest => quest.Id);

            var sanitized = incoming
                .Select(dto => Sanitize(dto, ownedMyGameIds))
                .Where(dto => !string.IsNullOrWhiteSpace(dto.Title))
                .ToList();

            var keepIds = new HashSet<int>();
            foreach (var (dto, index) in sanitized.Select((dto, index) => (dto, index)))
            {
                if (dto.Id > 0 && existingById.TryGetValue(dto.Id, out var existingQuest))
                {
                    ApplyTo(existingQuest, dto, index);
                    keepIds.Add(existingQuest.Id);
                }
                else
                {
                    var newQuest = new Quest { LuminaUserId = userId };
                    ApplyTo(newQuest, dto, index);
                    await dbContext.Quests.AddAsync(newQuest);
                }
            }

            var toDelete = existing.Where(quest => !keepIds.Contains(quest.Id)).ToList();
            if (toDelete.Count > 0)
            {
                dbContext.Quests.RemoveRange(toDelete);
            }
        }

        private static QuestDto Sanitize(QuestDto dto, List<int> ownedMyGameIds)
        {
            return new QuestDto
            {
                Id = dto.Id,
                Title = (dto.Title ?? string.Empty).Trim(),
                Type = dto.Type,
                RewardXp = dto.RewardXp <= 0 ? RewardFor(dto.Type) : dto.RewardXp,
                Completed = dto.Completed,
                CompletedAt = dto.Completed ? dto.CompletedAt ?? DateTime.UtcNow : null,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                SortOrder = dto.SortOrder,
                MyGameId = dto.MyGameId.HasValue && ownedMyGameIds.Contains(dto.MyGameId.Value)
                    ? dto.MyGameId
                    : null
            };
        }

        private static void ApplyTo(Quest quest, QuestDto dto, int fallbackSortOrder)
        {
            quest.Title = dto.Title;
            quest.Type = dto.Type;
            quest.RewardXp = dto.RewardXp;
            quest.Completed = dto.Completed;
            quest.CompletedAt = dto.CompletedAt;
            quest.CreatedAt = dto.CreatedAt;
            quest.SortOrder = dto.SortOrder == 0 ? fallbackSortOrder : dto.SortOrder;
            quest.MyGameId = dto.MyGameId;
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

        private static QuestBoardDto CreateDefaultBoard()
        {
            return new QuestBoardDto
            {
                Quests =
                [
                    new() { Id = -1, Title = "Define the next personal milestone", Type = QuestType.Main, RewardXp = RewardFor(QuestType.Main), SortOrder = 0 },
                    new() { Id = -2, Title = "Finish one meaningful project sprint", Type = QuestType.Main, RewardXp = RewardFor(QuestType.Main), SortOrder = 1 },
                    new() { Id = -3, Title = "Clear the desk before starting", Type = QuestType.Sub, RewardXp = RewardFor(QuestType.Sub), SortOrder = 0 },
                    new() { Id = -4, Title = "Plan tomorrow in three bullets", Type = QuestType.Sub, RewardXp = RewardFor(QuestType.Sub), Completed = true, CompletedAt = DateTime.UtcNow, SortOrder = 1 },
                    new() { Id = -5, Title = "Check in with someone you care about", Type = QuestType.Faction, RewardXp = RewardFor(QuestType.Faction), SortOrder = 0 }
                ],
                Skills =
                [
                    new()
                    {
                        Id = -11,
                        Name = "Programming",
                        Icon = "code-slash-outline",
                        Color = "#2563eb",
                        Xp = 120,
                        SortOrder = 0,
                        Nodes =
                        [
                            new() { Id = -111, Name = "Debugging", Unlocked = true, UnlockedAt = DateTime.UtcNow, SortOrder = 0 },
                            new() { Id = -112, Name = "Architecture", SortOrder = 1 },
                            new() { Id = -113, Name = "Shipping", SortOrder = 2 }
                        ]
                    },
                    new()
                    {
                        Id = -12,
                        Name = "Drawing",
                        Icon = "brush-outline",
                        Color = "#0891b2",
                        Xp = 60,
                        SortOrder = 1,
                        Nodes =
                        [
                            new() { Id = -121, Name = "Sketching", SortOrder = 0 },
                            new() { Id = -122, Name = "Color study", SortOrder = 1 },
                            new() { Id = -123, Name = "Finished piece", SortOrder = 2 }
                        ]
                    },
                    new()
                    {
                        Id = -13,
                        Name = "Cooking",
                        Icon = "restaurant-outline",
                        Color = "#0f766e",
                        Xp = 90,
                        SortOrder = 2,
                        Nodes =
                        [
                            new() { Id = -131, Name = "Knife basics", Unlocked = true, UnlockedAt = DateTime.UtcNow, SortOrder = 0 },
                            new() { Id = -132, Name = "Meal prep", SortOrder = 1 },
                            new() { Id = -133, Name = "Signature dish", SortOrder = 2 }
                        ]
                    }
                ]
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
