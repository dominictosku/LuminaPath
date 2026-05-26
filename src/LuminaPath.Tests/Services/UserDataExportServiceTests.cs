using System.Text.Json;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Test.Utilities;

namespace Test.Services;

public class UserDataExportServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 5, 26, 12, 34, 56, DateTimeKind.Utc);

    [Fact]
    public async Task ExportAsync_ReturnsOwnedLibraryQuestSessionAndAchievementData()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        const string userId = "user-1";

        await using (var dbContext = new LuminaPathDbContext(options))
        {
            var user = new LuminaUser
            {
                Id = userId,
                UserName = "anna",
                Email = "anna@example.com",
                FullName = "Anna Example",
                LuminaUserInfo = new LuminaUserInfo
                {
                    UserId = userId,
                    SteamId = "7656119",
                    SteamPersonaName = "Anna",
                    PSNOnlineId = "anna-psn"
                }
            };

            var game = new Game
            {
                Name = "Hades",
                Description = "Roguelike",
                Genres = ["Action", "Roguelike"],
                Platforms = Platforms.PC | Platforms.Switch,
                Playtime = 30,
                Source = "Steam",
                ExternalIds =
                [
                    new MediaExternalId
                    {
                        Provider = ExternalMediaProvider.Steam,
                        ExternalId = "1145360"
                    }
                ]
            };

            var myGame = new MyGame
            {
                Game = game,
                LuminaUserId = userId,
                Status = GameStatus.Playing,
                Priority = 2,
                Rating = 9,
                PersonalNotes = "Try heat 16",
                TimeSpend = 14.5,
                MyGameInfo = new MyGameInfo
                {
                    TrackedHours = 12.25,
                    FirstPlayed = FixedNow.AddDays(-20),
                    LastPlayed = FixedNow.AddDays(-1)
                }
            };

            var folder = new QuestFolder
            {
                LuminaUserId = userId,
                Name = "Bosses",
                Emoji = "!",
                SortOrder = 1,
                CreatedAt = FixedNow.AddDays(-5),
                UpdatedAt = FixedNow.AddDays(-2)
            };

            var skill = new QuestSkill
            {
                LuminaUserId = userId,
                Name = "Buildcraft",
                Icon = "code-slash-outline",
                Color = "#2563eb",
                Xp = 40,
                SortOrder = 0,
                Nodes =
                [
                    new QuestSkillNode
                    {
                        Name = "Dash timing",
                        Unlocked = true,
                        UnlockedAt = FixedNow.AddDays(-3),
                        SortOrder = 0
                    }
                ]
            };

            var quest = new Quest
            {
                LuminaUserId = userId,
                Title = "Defeat Megaera",
                Notes = "Use shield",
                Type = QuestType.Main,
                Priority = QuestPriority.High,
                Tags = ["boss", "hades"],
                RewardXp = 150,
                CreatedAt = FixedNow.AddDays(-4),
                UpdatedAt = FixedNow.AddDays(-1),
                MyGame = myGame,
                Skill = skill,
                QuestFolder = folder,
                Subtasks =
                [
                    new QuestSubtask
                    {
                        Title = "Upgrade dash",
                        SortOrder = 0,
                        CreatedAt = FixedNow.AddDays(-4)
                    }
                ]
            };

            var questProfile = new QuestProfile
            {
                LuminaUserId = userId,
                TotalXp = 150,
                CurrentStreakDays = 2,
                LongestStreakDays = 5,
                LastCompletionDate = DateOnly.FromDateTime(FixedNow),
                Achievements =
                [
                    new Achievement
                    {
                        Code = "first-clear",
                        UnlockedAt = FixedNow.AddDays(-1)
                    }
                ]
            };

            var gameAchievement = new GameAchievement
            {
                Game = game,
                CanonicalKey = "hades:escaped",
                Title = "Escaped",
                Description = "Escape the underworld.",
                PrimaryProvider = ExternalMediaProvider.Steam,
                SteamApiName = "ACH_ESCAPE"
            };

            dbContext.Users.Add(user);
            dbContext.MyGames.Add(myGame);
            dbContext.QuestProfiles.Add(questProfile);
            dbContext.QuestFolders.Add(folder);
            dbContext.QuestSkills.Add(skill);
            dbContext.Quests.Add(quest);
            dbContext.GamingSessions.Add(new GamingSession
            {
                LuminaUserId = userId,
                MyGame = myGame,
                ScheduledAt = FixedNow.AddDays(1),
                DurationMinutes = 90,
                Notes = "Boss attempts"
            });
            dbContext.GameAchievements.Add(gameAchievement);
            dbContext.UserGameAchievements.Add(new UserGameAchievement
            {
                LuminaUserId = userId,
                GameAchievement = gameAchievement,
                Provider = ExternalMediaProvider.Steam,
                SourceAchievementId = "ACH_ESCAPE",
                UnlockedAt = FixedNow.AddDays(-1),
                SyncedAt = FixedNow
            });
            dbContext.UserDocuments.Add(new UserDocument
            {
                UserId = userId,
                Album = "Screenshots",
                Name = "clear.png",
                StorageName = "clear-1.png",
                DocumentType = DocumentType.Image,
                ContentType = "image/png"
            });

            await dbContext.SaveChangesAsync();
        }

        var service = new UserDataExportService(new TestDbContextFactory(options), () => FixedNow);

        var export = await service.ExportAsync(userId);

        Assert.Equal("LuminaPath-export-20260526-123456Z.json", export.FileName);
        using var document = JsonDocument.Parse(export.Content);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Anna Example", root.GetProperty("user").GetProperty("fullName").GetString());
        Assert.Equal("7656119", root.GetProperty("user").GetProperty("steam").GetProperty("steamId").GetString());

        var gameExport = root.GetProperty("library").GetProperty("games")[0];
        Assert.Equal("Try heat 16", gameExport.GetProperty("personalNotes").GetString());
        Assert.Equal("Hades", gameExport.GetProperty("game").GetProperty("media").GetProperty("name").GetString());
        Assert.Equal("1145360", gameExport.GetProperty("game").GetProperty("media").GetProperty("externalIds")[0].GetProperty("externalId").GetString());

        var questExport = root.GetProperty("quests").GetProperty("items")[0];
        Assert.Equal("Defeat Megaera", questExport.GetProperty("title").GetString());
        Assert.Equal("Upgrade dash", questExport.GetProperty("subtasks")[0].GetProperty("title").GetString());

        Assert.Equal("Hades", root.GetProperty("gamingSessions")[0].GetProperty("gameName").GetString());
        Assert.Equal("Escaped", root.GetProperty("gameAchievements")[0].GetProperty("achievement").GetProperty("title").GetString());
        Assert.Equal("Screenshots", root.GetProperty("documents")[0].GetProperty("album").GetString());
    }
}
