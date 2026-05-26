using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ModelServices;
using Test.Utilities;

namespace Test.Services;

public class GlobalSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_FindsOwnedLibraryNotesAndQuests()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        const string userId = "user-1";

        await using (var dbContext = new LuminaPathDbContext(options))
        {
            var game = new Game
            {
                Name = "Hades",
                Description = "Escape the underworld.",
                Genres = ["Action", "Roguelike"],
                Platforms = Platforms.PC
            };
            var myGame = new MyGame
            {
                Game = game,
                LuminaUserId = userId,
                Status = GameStatus.Playing,
                Priority = 1,
                PersonalNotes = "Try a shield heat 16 route next."
            };

            dbContext.MyGames.Add(myGame);
            dbContext.Quests.Add(new Quest
            {
                LuminaUserId = userId,
                Title = "Beat the Bone Hydra",
                Notes = "Practice dash timing before the fight.",
                Type = QuestType.Main,
                Priority = QuestPriority.High,
                Tags = ["boss"],
                Subtasks =
                [
                    new QuestSubtask { Title = "Upgrade dash", SortOrder = 0 }
                ]
            });
            dbContext.Quests.Add(new Quest
            {
                LuminaUserId = "other-user",
                Title = "Private other result",
                Notes = "heat should not leak"
            });

            await dbContext.SaveChangesAsync();
        }

        var service = new GlobalSearchService(new TestDbContextFactory(options));

        var noteResults = await service.SearchAsync(userId, "heat");
        var questResults = await service.SearchAsync(userId, "dash");

        var note = Assert.Single(noteResults);
        Assert.Equal("note", note.Kind);
        Assert.Equal("Hades", note.Title);
        Assert.StartsWith("/library/games/", note.Route);
        Assert.Contains("Notes:", note.MatchedText);

        var quest = Assert.Single(questResults);
        Assert.Equal("quest", quest.Kind);
        Assert.Equal("Beat the Bone Hydra", quest.Title);
        Assert.Equal("/quests?questId=", quest.Route[..16]);
        Assert.Contains("dash", quest.MatchedText, StringComparison.OrdinalIgnoreCase);

        Assert.Empty(await service.SearchAsync(userId, "other"));
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForShortQueries()
    {
        var service = new GlobalSearchService(new TestDbContextFactory(Utilities.DbContext.TestDbContextOptions()));

        var results = await service.SearchAsync("user-1", "h");

        Assert.Empty(results);
    }
}
