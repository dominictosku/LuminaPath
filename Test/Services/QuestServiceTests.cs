using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services
{
    public class QuestServiceTests
    {
        [Fact]
        public async Task GetBoardAsync_ReturnsDefaultBoard_ForNewUser()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = new QuestService(new TestDbContextFactory(options));

            var board = await service.GetBoardAsync("new-user");

            Assert.Equal(5, board.Quests.Count);
            Assert.Equal(3, board.Skills.Count);
            Assert.All(board.Quests, quest => Assert.True(quest.Id < 0));
            Assert.All(board.Skills, skill => Assert.True(skill.Id < 0));
        }

        [Fact]
        public async Task SaveBoardAsync_SanitizesAndPersistsUserBoard()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(new LuminaUser
                {
                    Id = userId,
                    UserName = "test@example.com",
                    Email = "test@example.com"
                });
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var saved = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Xp = -50,
                Quests =
                [
                    new() { Title = "  Ship tests  ", Type = QuestType.Main, RewardXp = 0, Completed = true },
                    new() { Title = "   ", Type = QuestType.Sub, RewardXp = 75 }
                ],
                Skills =
                [
                    new()
                    {
                        Name = "  Stability  ",
                        Icon = " ",
                        Color = " ",
                        Xp = -10,
                        Nodes =
                        [
                            new() { Name = "  Service tests  ", Unlocked = true },
                            new() { Name = "   ", Unlocked = true }
                        ]
                    },
                    new() { Name = "   " }
                ]
            });

            Assert.Equal(0, saved.Xp);

            var quest = Assert.Single(saved.Quests);
            Assert.Equal("Ship tests", quest.Title);
            Assert.Equal(150, quest.RewardXp);
            Assert.True(quest.Completed);
            Assert.NotNull(quest.CompletedAt);

            var skill = Assert.Single(saved.Skills);
            Assert.Equal("Stability", skill.Name);
            Assert.Equal("code-slash-outline", skill.Icon);
            Assert.Equal("#2563eb", skill.Color);
            Assert.Equal(0, skill.Xp);

            var node = Assert.Single(skill.Nodes);
            Assert.Equal("Service tests", node.Name);
            Assert.True(node.Unlocked);
            Assert.NotNull(node.UnlockedAt);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, await assertContext.QuestProfiles.CountAsync(profile => profile.LuminaUserId == userId));
            Assert.Equal(1, await assertContext.Quests.CountAsync(quest => quest.LuminaUserId == userId));
            Assert.Equal(1, await assertContext.QuestSkills.CountAsync(skill => skill.LuminaUserId == userId));
            Assert.Equal(1, await assertContext.QuestSkillNodes.CountAsync());
        }
    }
}
