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

            var quests = await dbContext.Quests
                .AsNoTracking()
                .Where(quest => quest.LuminaUserId == userId)
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
                    SortOrder = quest.SortOrder
                })
                .ToListAsync();

            var skills = await dbContext.QuestSkills
                .AsNoTracking()
                .Include(skill => skill.Nodes)
                .Where(skill => skill.LuminaUserId == userId)
                .OrderBy(skill => skill.SortOrder)
                .ThenBy(skill => skill.Id)
                .ToListAsync();

            if (profile == null && quests.Count == 0 && skills.Count == 0)
            {
                return CreateDefaultBoard();
            }

            return new QuestBoardDto
            {
                Xp = profile?.TotalXp ?? 0,
                Quests = quests,
                Skills = skills.Select(skill => new QuestSkillDto
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
                }).ToList()
            };
        }

        public async Task<QuestBoardDto> SaveBoardAsync(string userId, QuestBoardDto board)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var profile = await dbContext.QuestProfiles.FirstOrDefaultAsync(profile => profile.LuminaUserId == userId);

            if (profile == null)
            {
                profile = new QuestProfile
                {
                    LuminaUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await dbContext.QuestProfiles.AddAsync(profile);
            }

            profile.TotalXp = Math.Max(0, board.Xp);
            profile.UpdatedAt = DateTime.UtcNow;

            var existingQuests = await dbContext.Quests
                .Where(quest => quest.LuminaUserId == userId)
                .ToListAsync();
            dbContext.Quests.RemoveRange(existingQuests);

            var existingSkills = await dbContext.QuestSkills
                .Include(skill => skill.Nodes)
                .Where(skill => skill.LuminaUserId == userId)
                .ToListAsync();
            dbContext.QuestSkills.RemoveRange(existingSkills);

            await dbContext.Quests.AddRangeAsync(board.Quests.Select((quest, index) => new Quest
            {
                Title = quest.Title.Trim(),
                Type = quest.Type,
                RewardXp = quest.RewardXp <= 0 ? RewardFor(quest.Type) : quest.RewardXp,
                Completed = quest.Completed,
                CompletedAt = quest.Completed ? quest.CompletedAt ?? DateTime.UtcNow : null,
                CreatedAt = quest.CreatedAt == default ? DateTime.UtcNow : quest.CreatedAt,
                SortOrder = quest.SortOrder == 0 ? index : quest.SortOrder,
                LuminaUserId = userId
            }).Where(quest => !string.IsNullOrWhiteSpace(quest.Title)));

            await dbContext.QuestSkills.AddRangeAsync(board.Skills.Select((skill, skillIndex) => new QuestSkill
            {
                Name = skill.Name.Trim(),
                Icon = string.IsNullOrWhiteSpace(skill.Icon) ? "code-slash-outline" : skill.Icon,
                Color = string.IsNullOrWhiteSpace(skill.Color) ? "#2563eb" : skill.Color,
                Xp = Math.Max(0, skill.Xp),
                SortOrder = skill.SortOrder == 0 ? skillIndex : skill.SortOrder,
                LuminaUserId = userId,
                Nodes = skill.Nodes.Select((node, nodeIndex) => new QuestSkillNode
                {
                    Name = node.Name.Trim(),
                    Unlocked = node.Unlocked,
                    UnlockedAt = node.Unlocked ? node.UnlockedAt ?? DateTime.UtcNow : null,
                    SortOrder = node.SortOrder == 0 ? nodeIndex : node.SortOrder
                }).Where(node => !string.IsNullOrWhiteSpace(node.Name)).ToList()
            }).Where(skill => !string.IsNullOrWhiteSpace(skill.Name)));

            await dbContext.SaveChangesAsync();
            return await GetBoardAsync(userId);
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
