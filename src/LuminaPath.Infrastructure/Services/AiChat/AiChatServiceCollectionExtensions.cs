using LuminaPath.Infrastructure.Configuration;
using LuminaPath.Infrastructure.Services.AiChat.Mcp;
using LuminaPath.Infrastructure.Services.AiChat.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.AiChat;

internal static class AiChatServiceCollectionExtensions
{
    public static IServiceCollection AddAiChatServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AiChatOptions>(config.GetSection(AiChatOptions.SectionName));
        services.Configure<AnthropicOptions>(config.GetSection(AnthropicOptions.SectionName));
        services.PostConfigure<AnthropicOptions>(opts =>
        {
            opts.ApiKey.UseEnvironmentFallback(value => opts.ApiKey = value, "ANTHROPIC_API_KEY");
        });

        services.Configure<OpenAiOptions>(config.GetSection(OpenAiOptions.SectionName));
        services.PostConfigure<OpenAiOptions>(opts =>
        {
            opts.ApiKey.UseEnvironmentFallback(value => opts.ApiKey = value, "OPENAI_API_KEY");

            var envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envBaseUrl))
            {
                opts.BaseUrl = envBaseUrl;
            }

            var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
            if (!string.IsNullOrWhiteSpace(envModel))
            {
                opts.Model = envModel;
            }
        });

        services.Configure<McpOptions>(config.GetSection(McpOptions.SectionName));
        services.PostConfigure<McpOptions>(opts =>
        {
            foreach (var server in opts.Servers)
            {
                foreach (var key in server.Env.Keys.ToList())
                {
                    server.Env[key].UseEnvironmentFallback(value => server.Env[key] = value, key);
                }
            }
        });

        services.AddHttpClient<AnthropicClient>();
        services.AddHttpClient<OpenAiCompatibleProvider>();
        services.AddScoped<AiChatRuntimeSettingsResolver>();
        services.AddScoped<IAiProvider, RuntimeAiProvider>();

        services.AddSingleton<McpHostService>();
        services.AddHostedService(sp => sp.GetRequiredService<McpHostService>());

        services.AddSingleton<IChatTool, ListUpcomingReleasesTool>();
        services.AddSingleton<IChatTool, SearchMyLibraryTool>();
        services.AddSingleton<IChatTool, MyQuestsTool>();
        services.AddSingleton<IChatTool, MyGamingSessionsTool>();
        services.AddSingleton<IChatTool, LibrarySummaryTool>();
        services.AddSingleton<IChatTool, CreateQuestTool>();

        services.AddSingleton<ChatToolRegistry>();
        services.AddScoped<ChatService>();
        return services;
    }
}
