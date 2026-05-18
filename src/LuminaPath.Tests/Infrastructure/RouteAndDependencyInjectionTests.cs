using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LuminaPath.Tests.Infrastructure;

public class RouteAndDependencyInjectionTests
{
    private static readonly HashSet<string> PublicIdentityApiRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET api/confirmEmail",
        "POST api/forgotPassword",
        "POST api/login",
        "POST api/refresh",
        "POST api/register",
        "POST api/resendConfirmationEmail",
        "POST api/resetPassword"
    };

    [Fact]
    public async Task ConfigureServer_MapsExpectedApiRoutes()
    {
        await using var app = BuildRoutedApp();
        var endpoints = GetRouteEndpoints(app);

        AssertRouteExists(endpoints, "GET", "api/Games");
        AssertRouteExists(endpoints, "GET", "api/Games/{id}");
        AssertRouteExists(endpoints, "POST", "api/Games");
        AssertRouteExists(endpoints, "PUT", "api/Games/{id}");
        AssertRouteExists(endpoints, "DELETE", "api/Games/{id}");
        AssertRouteExists(endpoints, "GET", "api/Games/{id:int}/news");
        AssertRouteExists(endpoints, "GET", "api/MyGames/{id:int}/achievements");
        AssertRouteExists(endpoints, "POST", "api/chat/stream");
        AssertRouteExists(endpoints, "GET", "api/browse/games/releases");
        AssertRouteExists(endpoints, "POST", "api/logout");
        AssertRouteExists(endpoints, "GET", "api/admin/database-backups/{fileName}/download");
        AssertRouteExists(endpoints, "GET", "health");
    }

    [Fact]
    public async Task ConfigureServer_HasNoDuplicateHttpRouteRegistrations()
    {
        await using var app = BuildRoutedApp();

        var duplicates = GetRouteEndpoints(app)
            .SelectMany(endpoint => GetHttpMethods(endpoint)
                .Select(method => new
                {
                    Key = $"{method} {NormalizeRoute(endpoint.RoutePattern.RawText)}",
                    Endpoint = endpoint
                }))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(item => item.Endpoint.DisplayName))}")
            .OrderBy(value => value)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public async Task ConfigureServer_ApiRoutesRequireAuthorizationOrKnownPublicAccess()
    {
        await using var app = BuildRoutedApp();

        var unprotectedApiRoutes = GetRouteEndpoints(app)
            .Where(endpoint => NormalizeRoute(endpoint.RoutePattern.RawText).StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            .Where(endpoint => !RequiresAuthorization(endpoint) && !AllowsAnonymous(endpoint))
            .Where(endpoint => !IsKnownPublicIdentityApiRoute(endpoint))
            .Select(DescribeEndpoint)
            .OrderBy(value => value)
            .ToList();

        Assert.Empty(unprotectedApiRoutes);
    }

    [Fact]
    public async Task ConfigureServer_AdminBackupDownloadRequiresAdministratorRole()
    {
        await using var app = BuildRoutedApp();

        var endpoint = GetRouteEndpoints(app)
            .Single(endpoint => RouteMatches(endpoint, "api/admin/database-backups/{fileName}/download")
                && GetHttpMethods(endpoint).Contains("GET"));

        Assert.True(RequiresAuthorization(endpoint));
        Assert.True(
            RequiresRole(endpoint, "Administrator"),
            $"Expected {DescribeEndpoint(endpoint)} to require the Administrator role.");
    }

    [Fact]
    public async Task AddInfrastructure_CanActivateEveryConcreteApiController()
    {
        var services = CreateServices();

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        var failures = ConcreteControllerTypes()
            .Select(type => TryActivateController(scope.ServiceProvider, type))
            .Where(result => result.Error is not null)
            .Select(result => $"{result.ControllerType.Name}: {result.Error!.GetType().Name} - {result.Error.Message}")
            .OrderBy(value => value)
            .ToList();

        Assert.Empty(failures);
    }

    private static WebApplication BuildRoutedApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(ServerEndpointRouteBuilderExtensions).Assembly.GetName().Name,
            EnvironmentName = Environments.Production,
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.Configuration.AddInMemoryCollection(CreateConfigurationValues());
        builder.Services.AddScoped<IObjectMapper, ObjectMapper>();
        builder.Services
            .AddInfrastructure(builder.Configuration)
            .AddServer();

        var app = builder.Build();
        app.ConfigureServer();
        return app;
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateConfigurationValues())
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddScoped<IObjectMapper, ObjectMapper>();
        services
            .AddInfrastructure(configuration)
            .AddServer();

        return services;
    }

    private static IReadOnlyList<RouteEndpoint> GetRouteEndpoints(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static void AssertRouteExists(
        IEnumerable<RouteEndpoint> endpoints,
        string method,
        string route)
    {
        Assert.Contains(endpoints, endpoint =>
            RouteMatches(endpoint, route)
            && GetHttpMethods(endpoint).Contains(method, StringComparer.OrdinalIgnoreCase));
    }

    private static bool RouteMatches(RouteEndpoint endpoint, string route)
    {
        return string.Equals(
            NormalizeRoute(endpoint.RoutePattern.RawText),
            NormalizeRoute(route),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRoute(string? route)
    {
        return (route ?? string.Empty).Trim('/');
    }

    private static IReadOnlyList<string> GetHttpMethods(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.ToList()
            ?? ["*"];
    }

    private static bool RequiresAuthorization(Endpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0
            || endpoint.Metadata.GetMetadata<AuthorizationPolicy>() is not null;
    }

    private static bool AllowsAnonymous(Endpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
    }

    private static bool IsKnownPublicIdentityApiRoute(RouteEndpoint endpoint)
    {
        var route = NormalizeRoute(endpoint.RoutePattern.RawText);
        return GetHttpMethods(endpoint).Any(method => PublicIdentityApiRoutes.Contains($"{method} {route}"));
    }

    private static bool RequiresRole(Endpoint endpoint, string role)
    {
        var rolesFromAuthorizeAttributes = endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .SelectMany(data => SplitRoles(data.Roles));

        if (rolesFromAuthorizeAttributes.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return endpoint.Metadata.GetMetadata<AuthorizationPolicy>()?.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .Any(requirement => requirement.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)) == true;
    }

    private static IEnumerable<string> SplitRoles(string? roles)
    {
        return roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
    }

    private static string DescribeEndpoint(RouteEndpoint endpoint)
    {
        var methods = string.Join(",", GetHttpMethods(endpoint));
        return $"{methods} /{NormalizeRoute(endpoint.RoutePattern.RawText)} ({endpoint.DisplayName})";
    }

    private static IEnumerable<Type> ConcreteControllerTypes()
    {
        return typeof(FilesController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .OrderBy(type => type.Name);
    }

    private static (Type ControllerType, Exception? Error) TryActivateController(IServiceProvider services, Type controllerType)
    {
        try
        {
            _ = ActivatorUtilities.CreateInstance(services, controllerType);
            return (controllerType, null);
        }
        catch (Exception ex)
        {
            return (controllerType, ex);
        }
    }

    private static Dictionary<string, string?> CreateConfigurationValues()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=luminapath_test;Username=test;Password=test",
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["Storage:Provider"] = "FileSystem",
            ["Storage:Path"] = "App_Data/storage",
            ["Cors:AllowedOrigins:0"] = "http://localhost",
            ["AiChat:Provider"] = "Anthropic"
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "LuminaPath.Tests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LuminaPath.Tests");
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
