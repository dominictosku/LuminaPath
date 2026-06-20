using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services
{
    public class QuestServiceTests
    {
        [Fact]
        public async Task GetBoardAsync_ReturnsEmptyBoard_ForNewUser()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = new QuestService(new TestDbContextFactory(options));

            var board = await service.GetBoardAsync("new-user");

            Assert.Equal(0, board.Xp);
            Assert.Empty(board.Quests);
            Assert.Empty(board.Skills);
            Assert.Empty(board.Achievements);
        }

        [Fact]
        public async Task CreateAsync_SanitizesPersistsAndLinksOwnedGameAndSkill()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;
            int otherMyGameId;
            int skillId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userId), NewUser("other-user"));
                var game = new Game { Name = "Hades", Description = "Roguelike" };
                var otherGame = new Game { Name = "Celeste", Description = "Platformer" };
                dbContext.Games.AddRange(game, otherGame);
                var myGame = new MyGame { Game = game, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1 };
                var otherMyGame = new MyGame { Game = otherGame, LuminaUserId = "other-user", Status = GameStatus.Playing, Priority = 1 };
                var skill = new QuestSkill { LuminaUserId = userId, Name = "Programming", Icon = "code-slash-outline", Color = "#2563eb" };
                dbContext.MyGames.AddRange(myGame, otherMyGame);
                dbContext.QuestSkills.Add(skill);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;
                otherMyGameId = otherMyGame.Id;
                skillId = skill.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var created = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "  Beat boss  ",
                Notes = "  Focus phase  ",
                Type = QuestType.Main,
                Priority = QuestPriority.High,
                Tags = [" boss ", "Boss", "", "run"],
                MyGameId = myGameId,
                SkillId = skillId
            }));

            Assert.Equal("Beat boss", created.Quest.Title);
            Assert.Equal("Focus phase", created.Quest.Notes);
            Assert.Equal(150, created.Quest.RewardXp);
            Assert.Equal(myGameId, created.Quest.MyGameId);
            Assert.Equal("Hades", created.Quest.GameName);
            Assert.Equal(skillId, created.Quest.SkillId);
            Assert.Equal(["boss", "run"], created.Quest.Tags);

            var injected = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "Try to inject",
                MyGameId = otherMyGameId
            }));
            Assert.Null(injected.Quest.MyGameId);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(2, await assertContext.Quests.CountAsync(quest => quest.LuminaUserId == userId));
        }

        [Fact]
        public async Task CreateAsync_PersistsScheduleAndAlignsDueDate()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            var start = new DateTime(2026, 6, 8, 18, 30, 0, DateTimeKind.Utc);
            var end = start.AddMinutes(90);

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var created = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "Evening boss run",
                ScheduledStartAt = start,
                ScheduledEndAt = end
            }));

            Assert.Equal(start, created.Quest.ScheduledStartAt);
            Assert.Equal(end, created.Quest.ScheduledEndAt);
            Assert.Equal(start.Date, created.Quest.DueDate);

            await using var assertContext = new LuminaPathDbContext(options);
            var persisted = await assertContext.Quests.SingleAsync(quest => quest.LuminaUserId == userId);
            Assert.Equal(start, persisted.ScheduledStartAt);
            Assert.Equal(end, persisted.ScheduledEndAt);
        }

        [Fact]
        public async Task UpdateAsync_CompletesQuestAwardsXpSkillXpAndSpawnsRecurringQuest()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int questId;
            int skillId;
            var dueDate = new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc);
            var scheduledStart = new DateTime(2026, 5, 16, 18, 0, 0, DateTimeKind.Utc);
            var scheduledEnd = scheduledStart.AddHours(2);

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var skill = new QuestSkill { LuminaUserId = userId, Name = "Programming", Icon = "code-slash-outline", Color = "#2563eb" };
                var folder = new QuestFolder { LuminaUserId = userId, Name = "Weekly", Emoji = "W", Color = "#7c3aed" };
                var quest = new Quest
                {
                    LuminaUserId = userId,
                    Title = "Practice",
                    Type = QuestType.Sub,
                    Priority = QuestPriority.Medium,
                    Recurrence = QuestRecurrence.Daily,
                    DueDate = dueDate,
                    ScheduledStartAt = scheduledStart,
                    ScheduledEndAt = scheduledEnd,
                    RewardXp = 75,
                    Skill = skill,
                    QuestFolder = folder,
                    Tags = []
                };
                dbContext.Quests.Add(quest);
                await dbContext.SaveChangesAsync();
                questId = quest.Id;
                skillId = skill.Id;
            }

            var now = new DateTime(2026, 5, 17, 12, 30, 0, DateTimeKind.Utc);
            var service = new QuestService(new TestDbContextFactory(options), () => now);
            var result = Success(await service.UpdateAsync(userId, questId, new QuestUpdateDto { Completed = true }));

            Assert.True(result.Quest.Completed);
            Assert.Equal(now, result.Quest.CompletedAt);
            Assert.Equal(75, result.TotalXp);
            Assert.Equal(1, result.CurrentStreakDays);
            Assert.Equal(15, result.AwardedSkillXp);
            Assert.Equal(skillId, result.AwardedSkillId);
            Assert.NotNull(result.SpawnedQuest);
            Assert.False(result.SpawnedQuest!.Completed);
            Assert.Equal(dueDate.AddDays(1), result.SpawnedQuest.DueDate);
            Assert.Equal(scheduledStart.AddDays(1), result.SpawnedQuest.ScheduledStartAt);
            Assert.Equal(scheduledEnd.AddDays(1), result.SpawnedQuest.ScheduledEndAt);
            Assert.Equal("Weekly", result.SpawnedQuest.FolderName);

            await using var assertContext = new LuminaPathDbContext(options);
            var persistedSkill = await assertContext.QuestSkills.SingleAsync(s => s.Id == skillId);
            Assert.Equal(15, persistedSkill.Xp);
        }

        [Fact]
        public async Task UpdateAsync_OccurrenceEditDoesNotLeakIntoNextRecurringQuest()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            var start = new DateTime(2026, 6, 16, 18, 0, 0, DateTimeKind.Utc);
            var service = new QuestService(new TestDbContextFactory(options));

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var created = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "Practice",
                Recurrence = QuestRecurrence.Weekly,
                ScheduledStartAt = start,
                ScheduledEndAt = start.AddHours(1)
            }));

            Assert.NotNull(created.Quest.QuestSeriesId);

            var movedStart = start.AddHours(2);
            var edited = Success(await service.UpdateAsync(userId, created.Quest.Id, new QuestUpdateDto
            {
                EditScope = QuestEditScope.Occurrence,
                Title = "One-off practice",
                ScheduledStartAt = movedStart,
                ScheduledEndAt = movedStart.AddHours(1)
            }));

            Assert.Equal("One-off practice", edited.Quest.Title);
            Assert.True(edited.Quest.OverridesQuestSeries);

            var completed = Success(await service.UpdateAsync(userId, created.Quest.Id, new QuestUpdateDto { Completed = true }));

            Assert.NotNull(completed.SpawnedQuest);
            Assert.Equal("Practice", completed.SpawnedQuest!.Title);
            Assert.Equal(start.AddDays(7), completed.SpawnedQuest.ScheduledStartAt);
            Assert.Equal(start.AddDays(7).AddHours(1), completed.SpawnedQuest.ScheduledEndAt);
            Assert.False(completed.SpawnedQuest.OverridesQuestSeries);
        }

        [Fact]
        public async Task UpdateAsync_SeriesEditUpdatesTemplateUsedByNextRecurringQuest()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            var start = new DateTime(2026, 6, 16, 18, 0, 0, DateTimeKind.Utc);
            var service = new QuestService(new TestDbContextFactory(options));

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var created = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "Practice",
                Recurrence = QuestRecurrence.Weekly,
                ScheduledStartAt = start,
                ScheduledEndAt = start.AddHours(1)
            }));

            var seriesStart = start.AddHours(2);
            var edited = Success(await service.UpdateAsync(userId, created.Quest.Id, new QuestUpdateDto
            {
                EditScope = QuestEditScope.Series,
                Title = "Updated practice",
                Recurrence = QuestRecurrence.Daily,
                ScheduledStartAt = seriesStart,
                ScheduledEndAt = seriesStart.AddMinutes(90)
            }));

            Assert.Equal("Updated practice", edited.Quest.Title);
            Assert.Equal(QuestRecurrence.Daily, edited.Quest.Recurrence);
            Assert.False(edited.Quest.OverridesQuestSeries);

            var completed = Success(await service.UpdateAsync(userId, created.Quest.Id, new QuestUpdateDto { Completed = true }));

            Assert.NotNull(completed.SpawnedQuest);
            Assert.Equal("Updated practice", completed.SpawnedQuest!.Title);
            Assert.Equal(seriesStart.AddDays(1), completed.SpawnedQuest.ScheduledStartAt);
            Assert.Equal(seriesStart.AddDays(1).AddMinutes(90), completed.SpawnedQuest.ScheduledEndAt);
            Assert.Equal(QuestRecurrence.Daily, completed.SpawnedQuest.Recurrence);
        }

        [Fact]
        public async Task MaterializeOccurrenceAsync_CreatesOneOffOccurrenceAndPromotesItOnCompletion()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            var start = new DateTime(2026, 6, 4, 18, 0, 0, DateTimeKind.Utc);
            var projectedDate = start.AddDays(7).Date;
            var movedStart = new DateTime(2026, 6, 12, 20, 0, 0, DateTimeKind.Utc);
            var service = new QuestService(new TestDbContextFactory(options));

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var created = Success(await service.CreateAsync(userId, new QuestCreateDto
            {
                Title = "Weekly reset",
                Recurrence = QuestRecurrence.Weekly,
                ScheduledStartAt = start,
                ScheduledEndAt = start.AddHours(1)
            }));

            var materialized = Success(await service.MaterializeOccurrenceAsync(userId, created.Quest.Id, new QuestOccurrenceCreateDto
            {
                OccurrenceDate = projectedDate,
                ScheduledStartAt = movedStart,
                ScheduledEndAt = movedStart.AddHours(1)
            }));

            Assert.NotEqual(created.Quest.Id, materialized.Quest.Id);
            Assert.Equal(created.Quest.QuestSeriesId, materialized.Quest.QuestSeriesId);
            Assert.Equal(projectedDate, materialized.Quest.SeriesOccurrenceDate);
            Assert.Equal(movedStart, materialized.Quest.ScheduledStartAt);
            Assert.Equal(movedStart.Date, materialized.Quest.DueDate);
            Assert.True(materialized.Quest.OverridesQuestSeries);
            Assert.False(materialized.Quest.ProjectsQuestSeries);

            var completed = Success(await service.UpdateAsync(userId, created.Quest.Id, new QuestUpdateDto { Completed = true }));

            Assert.NotNull(completed.SpawnedQuest);
            Assert.Equal(materialized.Quest.Id, completed.SpawnedQuest!.Id);
            Assert.Equal(movedStart, completed.SpawnedQuest.ScheduledStartAt);
            Assert.True(completed.SpawnedQuest.ProjectsQuestSeries);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(2, await assertContext.Quests.CountAsync(quest => quest.LuminaUserId == userId));
        }

        [Fact]
        public async Task UpdateAsync_ReschedulesAndClearsQuestBlock()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int questId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var quest = NewQuest(userId, "Plan dungeon", QuestType.Sub, 0);
                dbContext.Quests.Add(quest);
                await dbContext.SaveChangesAsync();
                questId = quest.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var start = new DateTime(2026, 6, 9, 19, 0, 0, DateTimeKind.Utc);
            var scheduled = Success(await service.UpdateAsync(userId, questId, new QuestUpdateDto
            {
                ScheduledStartAt = start,
                ScheduledEndAt = start.AddMinutes(45)
            }));

            Assert.Equal(start, scheduled.Quest.ScheduledStartAt);
            Assert.Equal(start.AddMinutes(45), scheduled.Quest.ScheduledEndAt);
            Assert.Equal(start.Date, scheduled.Quest.DueDate);

            var cleared = Success(await service.UpdateAsync(userId, questId, new QuestUpdateDto { ClearSchedule = true }));

            Assert.Null(cleared.Quest.ScheduledStartAt);
            Assert.Null(cleared.Quest.ScheduledEndAt);
            Assert.Equal(start.Date, cleared.Quest.DueDate);
        }

        [Fact]
        public async Task ReorderAsync_UpdatesSortOrderAndTypeForOwnedQuestsOnly()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int firstId;
            int secondId;
            int otherId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser(userId), NewUser("other-user"));
                var first = NewQuest(userId, "First", QuestType.Sub, 0);
                var second = NewQuest(userId, "Second", QuestType.Sub, 1);
                var other = NewQuest("other-user", "Other", QuestType.Sub, 0);
                dbContext.Quests.AddRange(first, second, other);
                await dbContext.SaveChangesAsync();
                firstId = first.Id;
                secondId = second.Id;
                otherId = other.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var count = Success(await service.ReorderAsync(userId,
            [
                new() { Id = secondId, SortOrder = 0, Type = QuestType.Main },
                new() { Id = firstId, SortOrder = 1, Type = QuestType.Faction },
                new() { Id = otherId, SortOrder = 99, Type = QuestType.Main }
            ]));

            Assert.Equal(3, count);
            await using var assertContext = new LuminaPathDbContext(options);
            var persistedFirst = await assertContext.Quests.SingleAsync(q => q.Id == firstId);
            var persistedSecond = await assertContext.Quests.SingleAsync(q => q.Id == secondId);
            var persistedOther = await assertContext.Quests.SingleAsync(q => q.Id == otherId);
            Assert.Equal(1, persistedFirst.SortOrder);
            Assert.Equal(QuestType.Faction, persistedFirst.Type);
            Assert.Equal(0, persistedSecond.SortOrder);
            Assert.Equal(QuestType.Main, persistedSecond.Type);
            Assert.Equal(0, persistedOther.SortOrder);
            Assert.Equal(QuestType.Sub, persistedOther.Type);
        }

        [Fact]
        public async Task SaveSkillsAsync_SanitizesUpsertsNodesAndRemovesOmittedSkills()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var first = await service.SaveSkillsAsync(userId, new QuestBoardDto
            {
                Xp = -10,
                Skills =
                [
                    new()
                    {
                        Name = "  Stability  ",
                        Icon = " ",
                        Color = " ",
                        Xp = -5,
                        Nodes =
                        [
                            new() { Name = "  Service tests  ", Unlocked = true },
                            new() { Name = " " }
                        ]
                    },
                    new() { Name = "Drawing" }
                ]
            });

            Assert.Equal(0, first.Xp);
            Assert.Equal(2, first.Skills.Count);
            var stability = first.Skills.Single(skill => skill.Name == "Stability");
            Assert.Equal("code-slash-outline", stability.Icon);
            Assert.Equal("#2563eb", stability.Color);
            Assert.Equal(0, stability.Xp);
            var node = Assert.Single(stability.Nodes);
            Assert.Equal("Service tests", node.Name);
            Assert.True(node.Unlocked);
            Assert.NotNull(node.UnlockedAt);

            var second = await service.SaveSkillsAsync(userId, new QuestBoardDto
            {
                Xp = 120,
                Skills =
                [
                    new()
                    {
                        Id = stability.Id,
                        Name = "Stability+",
                        Icon = "book-outline",
                        Color = "#0891b2",
                        Xp = 30,
                        Nodes = [new() { Id = node.Id, Name = "Regression tests", Unlocked = false }]
                    }
                ]
            });

            var updated = Assert.Single(second.Skills);
            Assert.Equal("Stability+", updated.Name);
            Assert.Equal(120, second.Xp);
            Assert.Equal("Regression tests", Assert.Single(updated.Nodes).Name);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, await assertContext.QuestSkills.CountAsync(skill => skill.LuminaUserId == userId));
            Assert.Equal(1, await assertContext.QuestSkillNodes.CountAsync());
        }

        [Fact]
        public async Task SubtaskMethods_AddUpdateAndDeleteOwnedSubtasks()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int questId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var quest = NewQuest(userId, "Ship", QuestType.Main, 0);
                dbContext.Quests.Add(quest);
                await dbContext.SaveChangesAsync();
                questId = quest.Id;
            }

            var service = new QuestService(new TestDbContextFactory(options));
            var added = Success(await service.AddSubtaskAsync(userId, questId, new QuestSubtaskCreateDto { Title = "  Write test  " }));
            var subtask = Assert.Single(added.Quest.Subtasks);
            Assert.Equal("Write test", subtask.Title);
            Assert.False(subtask.Completed);

            var updated = Success(await service.UpdateSubtaskAsync(userId, questId, subtask.Id, new QuestSubtaskUpdateDto
            {
                Title = "Write regression test",
                Completed = true,
                SortOrder = 4
            }));
            var updatedSubtask = Assert.Single(updated.Quest.Subtasks);
            Assert.True(updatedSubtask.Completed);
            Assert.NotNull(updatedSubtask.CompletedAt);
            Assert.Equal(4, updatedSubtask.SortOrder);

            var deletedId = Success(await service.DeleteSubtaskAsync(userId, questId, subtask.Id));
            Assert.Equal(subtask.Id, deletedId);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Empty(await assertContext.QuestSubtasks.ToListAsync());
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
                var game = new Game { Name = "Hades", Description = "Roguelike" };
                dbContext.Games.Add(game);
                var myGame = new MyGame { Game = game, LuminaUserId = userA, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                aMyGameId = myGame.Id;
                dbContext.Quests.AddRange(
                    NewQuest(userA, "Beat boss", QuestType.Main, 0, aMyGameId),
                    NewQuest(userA, "Side quest", QuestType.Sub, 1, aMyGameId),
                    NewQuest(userA, "Life todo", QuestType.Sub, 2));
                await dbContext.SaveChangesAsync();
            }

            var service = new QuestService(new TestDbContextFactory(options));

            var ownerQuests = await service.GetQuestsForMyGameAsync(userA, aMyGameId);
            Assert.Equal(2, ownerQuests.Count);
            Assert.All(ownerQuests, quest => Assert.Equal(aMyGameId, quest.MyGameId));
            Assert.All(ownerQuests, quest => Assert.Equal("Hades", quest.GameName));

            var nonOwnerQuests = await service.GetQuestsForMyGameAsync(userB, aMyGameId);
            Assert.Empty(nonOwnerQuests);
        }

        private static T Success<T>(Result<T, FailedResult> result)
        {
            Assert.True(result.IsSuccess, result.Match(_ => string.Empty, failure => string.Join("; ", failure.errorMessage)));
            return result.Match(value => value, failure => throw new InvalidOperationException(string.Join("; ", failure.errorMessage)));
        }

        private static Quest NewQuest(string userId, string title, QuestType type, int sortOrder, int? myGameId = null) => new()
        {
            LuminaUserId = userId,
            Title = title,
            Type = type,
            Priority = QuestPriority.Medium,
            Recurrence = QuestRecurrence.None,
            RewardXp = type == QuestType.Main ? 150 : type == QuestType.Faction ? 100 : 75,
            SortOrder = sortOrder,
            MyGameId = myGameId,
            Tags = []
        };

        private static LuminaUser NewUser(string id) => new()
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test"
        };
    }
}
