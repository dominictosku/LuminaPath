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
                dbContext.Users.Add(NewUser(userId));
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

        [Fact]
        public async Task SaveBoardAsync_UpsertsByIdAndPreservesQuestRowsForUnchangedQuests()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));

            var first = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Xp = 100,
                Quests =
                [
                    new() { Title = "Keep me", Type = QuestType.Main },
                    new() { Title = "Edit me", Type = QuestType.Sub }
                ]
            });

            var keepId = first.Quests.Single(quest => quest.Title == "Keep me").Id;
            var editId = first.Quests.Single(quest => quest.Title == "Edit me").Id;

            var second = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Xp = 175,
                Quests =
                [
                    new() { Id = keepId, Title = "Keep me", Type = QuestType.Main, RewardXp = 150 },
                    new() { Id = editId, Title = "Renamed", Type = QuestType.Sub, RewardXp = 75, Completed = true },
                    new() { Title = "Brand new", Type = QuestType.Faction }
                ]
            });

            Assert.Equal(175, second.Xp);
            Assert.Equal(3, second.Quests.Count);

            var keep = Assert.Single(second.Quests, quest => quest.Title == "Keep me");
            Assert.Equal(keepId, keep.Id);

            var edited = Assert.Single(second.Quests, quest => quest.Title == "Renamed");
            Assert.Equal(editId, edited.Id);
            Assert.True(edited.Completed);
            Assert.NotNull(edited.CompletedAt);

            var added = Assert.Single(second.Quests, quest => quest.Title == "Brand new");
            Assert.True(added.Id > 0);
            Assert.NotEqual(keepId, added.Id);
            Assert.NotEqual(editId, added.Id);
        }

        [Fact]
        public async Task SaveBoardAsync_RemovesQuestsThatAreOmittedFromTheNextSave()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));

            var first = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Quests =
                [
                    new() { Title = "Stays", Type = QuestType.Main },
                    new() { Title = "Goes away", Type = QuestType.Sub }
                ]
            });

            var staysId = first.Quests.Single(quest => quest.Title == "Stays").Id;

            await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Quests = [new() { Id = staysId, Title = "Stays", Type = QuestType.Main }]
            });

            await using var assertContext = new LuminaPathDbContext(options);
            var remaining = await assertContext.Quests.Where(quest => quest.LuminaUserId == userId).ToListAsync();
            var only = Assert.Single(remaining);
            Assert.Equal(staysId, only.Id);
            Assert.Equal("Stays", only.Title);
        }

        [Fact]
        public async Task SaveBoardAsync_LinksQuestToOwnedMyGame_AndDropsLinkToOthersGames()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";
            int aMyGameId;
            int bMyGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var aGame = new Game { Name = "Hades", Description = "Roguelike" };
                var bGame = new Game { Name = "Celeste", Description = "Platformer" };
                dbContext.Games.AddRange(aGame, bGame);
                var aMyGame = new MyGame { Game = aGame, LuminaUserId = userA, Status = GameStatus.Playing, Priority = 1 };
                var bMyGame = new MyGame { Game = bGame, LuminaUserId = userB, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.AddRange(aMyGame, bMyGame);
                await dbContext.SaveChangesAsync();
                aMyGameId = aMyGame.Id;
                bMyGameId = bMyGame.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));

            var saved = await service.SaveBoardAsync(userA, new QuestBoardDto
            {
                Quests =
                [
                    new() { Title = "Beat boss", Type = QuestType.Main, MyGameId = aMyGameId },
                    new() { Title = "Try to inject", Type = QuestType.Main, MyGameId = bMyGameId },
                    new() { Title = "Real life todo", Type = QuestType.Sub }
                ]
            });

            var linked = Assert.Single(saved.Quests, quest => quest.Title == "Beat boss");
            Assert.Equal(aMyGameId, linked.MyGameId);
            Assert.Equal("Hades", linked.GameName);

            var stripped = Assert.Single(saved.Quests, quest => quest.Title == "Try to inject");
            Assert.Null(stripped.MyGameId);

            var lifeTodo = Assert.Single(saved.Quests, quest => quest.Title == "Real life todo");
            Assert.Null(lifeTodo.MyGameId);
        }

        [Fact]
        public async Task SaveBoardAsync_NullsLinkWhenMyGameIsRemovedAfterPreviousSave()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hades", Description = "Roguelike" };
                dbContext.Games.Add(game);
                var myGame = new MyGame { Game = game, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));

            var first = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Quests = [new() { Title = "Beat boss", Type = QuestType.Main, MyGameId = myGameId }]
            });
            var questId = first.Quests.Single().Id;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                var myGame = await dbContext.MyGames.SingleAsync(g => g.Id == myGameId);
                dbContext.MyGames.Remove(myGame);
                await dbContext.SaveChangesAsync();
            }

            var second = await service.SaveBoardAsync(userId, new QuestBoardDto
            {
                Quests = [new() { Id = questId, Title = "Beat boss", Type = QuestType.Main, MyGameId = myGameId }]
            });

            var quest = Assert.Single(second.Quests);
            Assert.Equal(questId, quest.Id);
            Assert.Null(quest.MyGameId);
            Assert.Null(quest.GameName);
        }

        [Fact]
        public async Task GetQuestsForMyGameAsync_ReturnsOnlyLinkedQuestsForOwner()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userA = "user-a";
            const string userB = "user-b";
            int aMyGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userA), NewUser(userB));
                var aGame = new Game { Name = "Hades", Description = "Roguelike" };
                dbContext.Games.Add(aGame);
                var aMyGame = new MyGame { Game = aGame, LuminaUserId = userA, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(aMyGame);
                await dbContext.SaveChangesAsync();
                aMyGameId = aMyGame.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));

            await service.SaveBoardAsync(userA, new QuestBoardDto
            {
                Quests =
                [
                    new() { Title = "Beat boss", Type = QuestType.Main, MyGameId = aMyGameId },
                    new() { Title = "Side quest", Type = QuestType.Sub, MyGameId = aMyGameId },
                    new() { Title = "Life todo", Type = QuestType.Sub }
                ]
            });

            var ownerQuests = await service.GetQuestsForMyGameAsync(userA, aMyGameId);
            Assert.Equal(2, ownerQuests.Count);
            Assert.All(ownerQuests, quest => Assert.Equal(aMyGameId, quest.MyGameId));
            Assert.All(ownerQuests, quest => Assert.Equal("Hades", quest.GameName));

            var nonOwnerQuests = await service.GetQuestsForMyGameAsync(userB, aMyGameId);
            Assert.Empty(nonOwnerQuests);
        }

        private static LuminaUser NewUser(string id) => new()
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test"
        };
    }
}
