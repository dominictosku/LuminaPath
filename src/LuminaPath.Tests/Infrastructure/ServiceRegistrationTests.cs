using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.Imports;
using LuminaPath.Infrastructure.Services.ModelServices;
using LuminaPath.Core.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LuminaPath.Tests.Infrastructure;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddInfrastructureRegistersBackendFeatureServices()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddScoped<IObjectMapper, ObjectMapper>();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInfrastructure(configuration)
            .AddServer();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        Assert.NotNull(scopedServices.GetRequiredService<GameService>());
        Assert.NotNull(scopedServices.GetRequiredService<MyGameService>());
        Assert.NotNull(scopedServices.GetRequiredService<QuestService>());
        Assert.NotNull(scopedServices.GetRequiredService<DocumentService>());
        Assert.NotNull(scopedServices.GetRequiredService<AuditLogService>());
        Assert.NotNull(scopedServices.GetRequiredService<ExcelService>());
        Assert.NotNull(scopedServices.GetRequiredService<GameImportPipeline>());
        Assert.NotNull(scopedServices.GetRequiredService<BrowseLibraryService>());
        Assert.NotNull(scopedServices.GetRequiredService<LibraryIntegrityDiagnosticsService>());
        Assert.NotNull(scopedServices.GetRequiredService<DatabaseBackupService>());
        Assert.NotNull(scopedServices.GetRequiredService<ApplicationSettingsService>());
        Assert.NotNull(scopedServices.GetRequiredService<AiChatRuntimeSettingsResolver>());
        Assert.IsType<RuntimeAiProvider>(scopedServices.GetRequiredService<IAiProvider>());
        Assert.NotNull(scopedServices.GetRequiredService<BackgroundJobService>());
        Assert.NotEmpty(scopedServices.GetRequiredService<IEnumerable<IBackgroundJobRunner>>());
        Assert.NotNull(provider.GetRequiredService<IBackgroundJobQueue>());
        Assert.NotNull(provider.GetRequiredService<IBackgroundJobCancellationRegistry>());

        var rateLimiterOptions = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        Assert.NotNull(rateLimiterOptions.GlobalLimiter);
        Assert.Equal(StatusCodes.Status429TooManyRequests, rateLimiterOptions.RejectionStatusCode);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=luminapath_test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Storage:Provider"] = "FileSystem",
                ["Storage:Path"] = "App_Data/storage",
                ["Cors:AllowedOrigins:0"] = "http://localhost"
            })
            .Build();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "LuminaPath.Tests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LuminaPath.Tests");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
