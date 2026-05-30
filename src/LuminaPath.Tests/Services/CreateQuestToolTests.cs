using System.Text.Json;
using LuminaPath.Core.Enums;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.AiChat.Tools;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Utilities;

namespace LuminaPath.Tests.Services;

public class CreateQuestToolTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesQuestScopedToTheCurrentUser()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var tool = new CreateQuestTool();

        var json = await tool.ExecuteAsync(
            Args("""{ "title": "  Beat the final boss  ", "type": "Main", "priority": "High" }"""),
            Context(options, "user-1"),
            CancellationToken.None);

        using var result = JsonDocument.Parse(json);
        Assert.True(result.RootElement.GetProperty("created").GetBoolean());
        var quest = result.RootElement.GetProperty("quest");
        Assert.Equal("Beat the final boss", quest.GetProperty("title").GetString());
        Assert.Equal("Main", quest.GetProperty("type").GetString());
        Assert.Equal("High", quest.GetProperty("priority").GetString());

        await using var db = new LuminaPathDbContext(options);
        var persisted = await db.Quests.SingleAsync();
        Assert.Equal("Beat the final boss", persisted.Title);
        Assert.Equal("user-1", persisted.LuminaUserId);
        Assert.Equal(QuestType.Main, persisted.Type);
        Assert.False(persisted.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_BlankTitle_ReturnsErrorAndPersistsNothing()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var tool = new CreateQuestTool();

        var json = await tool.ExecuteAsync(
            Args("""{ "title": "   " }"""),
            Context(options, "user-1"),
            CancellationToken.None);

        using var result = JsonDocument.Parse(json);
        Assert.False(result.RootElement.GetProperty("created").GetBoolean());
        Assert.Contains("title", result.RootElement.GetProperty("error").GetString()!, StringComparison.OrdinalIgnoreCase);

        await using var db = new LuminaPathDbContext(options);
        Assert.Equal(0, await db.Quests.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_UnknownEnumValues_FallBackToDefaults()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var tool = new CreateQuestTool();

        await tool.ExecuteAsync(
            Args("""{ "title": "Tidy desk", "type": "Nonsense", "priority": "Whatever" }"""),
            Context(options, "user-1"),
            CancellationToken.None);

        await using var db = new LuminaPathDbContext(options);
        var persisted = await db.Quests.SingleAsync();
        Assert.Equal(QuestType.Sub, persisted.Type);
        Assert.Equal(QuestPriority.Medium, persisted.Priority);
    }

    [Fact]
    public async Task ExecuteAsync_ParsesDueDateAsUtc()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var tool = new CreateQuestTool();

        await tool.ExecuteAsync(
            Args("""{ "title": "Submit taxes", "due_date": "2026-04-15" }"""),
            Context(options, "user-1"),
            CancellationToken.None);

        await using var db = new LuminaPathDbContext(options);
        var persisted = await db.Quests.SingleAsync();
        Assert.NotNull(persisted.DueDate);
        Assert.Equal(new DateOnly(2026, 4, 15), DateOnly.FromDateTime(persisted.DueDate!.Value));
    }

    private static JsonElement Args(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static ChatToolContext Context(DbContextOptions<LuminaPathDbContext> options, string userId)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbContextFactory<LuminaPathDbContext>>(new TestDbContextFactory(options));
        services.AddScoped<QuestService>();
        return new ChatToolContext { UserId = userId, Services = services.BuildServiceProvider() };
    }
}
