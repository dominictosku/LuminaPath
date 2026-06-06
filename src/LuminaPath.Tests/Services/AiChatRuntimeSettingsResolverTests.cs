using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.AiChat;
using Microsoft.Extensions.Options;
using Test.Utilities;

namespace Test.Services;

public class AiChatRuntimeSettingsResolverTests
{
    [Fact]
    public async Task GetAsync_UsesStoredValuesWhenPresent()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.AiChatEnabled, Value = "false" },
                new ApplicationSetting { Key = ApplicationSettingsService.AiChatProvider, Value = "openai" },
                new ApplicationSetting { Key = ApplicationSettingsService.AiChatMaxToolIterations, Value = "64" },
                new ApplicationSetting { Key = ApplicationSettingsService.AiChatEnableMcpTools, Value = "false" },
                new ApplicationSetting { Key = ApplicationSettingsService.AiChatEnableWriteTools, Value = "false" },
                new ApplicationSetting { Key = ApplicationSettingsService.OpenAiBaseUrl, Value = "http://localhost:1234/v1" },
                new ApplicationSetting { Key = ApplicationSettingsService.OpenAiModel, Value = "local-model" },
                new ApplicationSetting { Key = ApplicationSettingsService.OpenAiMaxTokens, Value = "128" });
            await context.SaveChangesAsync();
        }

        var resolver = CreateResolver(options);

        var settings = await resolver.GetAsync();

        Assert.False(settings.Enabled);
        Assert.Equal("openai", settings.Provider);
        Assert.Equal(32, settings.MaxToolIterations);
        Assert.False(settings.EnableMcpTools);
        Assert.False(settings.EnableWriteTools);
        Assert.Equal("http://localhost:1234/v1", settings.OpenAi.BaseUrl);
        Assert.Equal("local-model", settings.OpenAi.Model);
        Assert.Equal(256, settings.OpenAi.MaxTokens);
    }

    [Fact]
    public async Task GetAsync_FallsBackToConfiguredOptionsWhenDatabaseIsEmpty()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var resolver = CreateResolver(options);

        var settings = await resolver.GetAsync();

        Assert.True(settings.Enabled);
        Assert.Equal("anthropic", settings.Provider);
        Assert.True(settings.EnableMcpTools);
        Assert.True(settings.EnableWriteTools);
        Assert.Equal("claude-test", settings.Anthropic.Model);
        Assert.Equal("openai-test", settings.OpenAi.Model);
    }

    private static AiChatRuntimeSettingsResolver CreateResolver(
        Microsoft.EntityFrameworkCore.DbContextOptions<LuminaPathDbContext> options)
    {
        var settingsService = new ApplicationSettingsService(new TestDbContextFactory(options));
        return new AiChatRuntimeSettingsResolver(
            Options.Create(new AiChatOptions { Provider = "anthropic", MaxToolIterations = 4 }),
            Options.Create(new AnthropicOptions { ApiKey = "anthropic-key", Model = "claude-test" }),
            Options.Create(new OpenAiOptions { ApiKey = "openai-key", Model = "openai-test" }),
            settingsService);
    }
}
