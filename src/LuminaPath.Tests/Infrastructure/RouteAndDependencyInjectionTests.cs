using LuminaPath;
using LuminaPath.Core.Mapping;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
    public void AddBlazor_DefaultsDetailedErrorsOff()
    {
        var services = CreateBlazorServices();
        services.AddBlazor();

        using var provider = services.BuildServiceProvider();

        Assert.False(provider.GetRequiredService<IOptions<CircuitOptions>>().Value.DetailedErrors);
    }

    [Fact]
    public void AddBlazor_EnablesDetailedErrors_WhenRequested()
    {
        var services = CreateBlazorServices();
        services.AddBlazor(detailedErrors: true);

        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<CircuitOptions>>().Value.DetailedErrors);
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
    public async Task ConfigureServer_MvcControllerRoutesRequireAuthorization()
    {
        await using var app = BuildRoutedApp();

        var unprotectedControllerRoutes = GetRouteEndpoints(app)
            .Where(IsMvcControllerEndpoint)
            .Where(endpoint => !RequiresAuthorization(endpoint) || AllowsAnonymous(endpoint))
            .Select(DescribeEndpoint)
            .OrderBy(value => value)
            .ToList();

        Assert.Empty(unprotectedControllerRoutes);
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
    public async Task ConfigureServer_FileUploadRequiresCatalogEditorsPolicy()
    {
        await using var app = BuildRoutedApp();

        var endpoint = GetRouteEndpoints(app)
            .Single(endpoint => RouteMatches(endpoint, "api/Files")
                && GetHttpMethods(endpoint).Contains("POST"));

        Assert.True(RequiresPolicy(endpoint, AuthorizationPolicies.CatalogEditors));
    }

    [Fact]
    public async Task ConfigureServer_CatalogMediaMutationRoutesRequireCatalogEditorsPolicy()
    {
        await using var app = BuildRoutedApp();
        var endpoints = GetRouteEndpoints(app);

        var expected = new[]
        {
            ("POST", "api/Games"),
            ("PUT", "api/Games/{id}"),
            ("DELETE", "api/Games/{id}"),
            ("POST", "api/Animes"),
            ("PUT", "api/Animes/{id}"),
            ("DELETE", "api/Animes/{id}"),
            ("POST", "api/Movies"),
            ("PUT", "api/Movies/{id}"),
            ("DELETE", "api/Movies/{id}"),
            ("POST", "api/Series"),
            ("PUT", "api/Series/{id}"),
            ("DELETE", "api/Series/{id}")
        };

        var missingPolicy = expected
            .Select(route => new
            {
                route,
                Endpoint = endpoints.Single(endpoint => RouteMatches(endpoint, route.Item2)
                    && GetHttpMethods(endpoint).Contains(route.Item1))
            })
            .Where(item => !RequiresPolicy(item.Endpoint, AuthorizationPolicies.CatalogEditors))
            .Select(item => $"{item.route.Item1} /{NormalizeRoute(item.route.Item2)}")
            .ToList();

        Assert.Empty(missingPolicy);
    }

    [Fact]
    public void CatalogEditorsPolicy_AllowsOnlyAdministratorOrEditor()
    {
        var services = CreateServices();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value
            .GetPolicy(AuthorizationPolicies.CatalogEditors);

        Assert.NotNull(policy);
        var roles = policy!.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .SelectMany(requirement => requirement.AllowedRoles)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(["Administrator", "Editor"], roles.OrderBy(role => role).ToArray());
    }

    [Fact]
    public void DocumentsPageRequiresAdministratorOrEditorRole()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "LuminaPath",
            "Features",
            "Documents",
            "Index.razor"));

        Assert.Contains("@attribute [Authorize(Roles = \"Administrator, Editor\")]", source);
    }

    [Fact]
    public async Task UseBlazor_MapsAnonymousBrowserFallback()
    {
        await using var app = BuildBlazorRoutedApp();

        var endpoint = GetRouteEndpoints(app)
            .SingleOrDefault(endpoint => string.Equals(
                NormalizeRoute(endpoint.RoutePattern.RawText),
                "{*path:nonfile}",
                StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(endpoint);
        Assert.True(AllowsAnonymous(endpoint!));
    }

    [Fact]
    public async Task ApplicationCookie_RedirectsBrowserChallengesAndKeepsApiStatusCodes()
    {
        var services = CreateServices();

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal("/Account/Login", options.LoginPath);
        Assert.Equal("/Account/AccessDenied", options.AccessDeniedPath);

        var apiLoginContext = CreateRedirectContext(provider, options, "/api/Games", "/Account/Login?ReturnUrl=%2Fapi%2FGames");
        await options.Events.RedirectToLogin(apiLoginContext);
        Assert.Equal(StatusCodes.Status401Unauthorized, apiLoginContext.Response.StatusCode);

        var apiAccessDeniedContext = CreateRedirectContext(provider, options, "/api/admin/users", "/Account/AccessDenied?ReturnUrl=%2Fapi%2Fadmin%2Fusers");
        await options.Events.RedirectToAccessDenied(apiAccessDeniedContext);
        Assert.Equal(StatusCodes.Status403Forbidden, apiAccessDeniedContext.Response.StatusCode);

        var browserLoginContext = CreateRedirectContext(provider, options, "/Admin/Overview", "/Account/Login?ReturnUrl=%2FAdmin%2FOverview");
        await options.Events.RedirectToLogin(browserLoginContext);
        Assert.Equal(StatusCodes.Status302Found, browserLoginContext.Response.StatusCode);
        Assert.Equal("/Account/Login?ReturnUrl=%2FAdmin%2FOverview", browserLoginContext.Response.Headers.Location);
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

    private static WebApplication BuildBlazorRoutedApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = Environments.Production,
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.Configuration.AddInMemoryCollection(CreateConfigurationValues());
        builder.Services.AddScoped<IObjectMapper, ObjectMapper>();
        builder.Services
            .AddInfrastructure(builder.Configuration)
            .AddServer()
            .AddBlazor();

        var app = builder.Build();
        app.ConfigureServer();
        app.UseBlazor();
        return app;
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateConfigurationValues())
            .Build();
        var environment = new TestHostEnvironment();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddScoped<IObjectMapper, ObjectMapper>();
        services
            .AddInfrastructure(configuration)
            .AddServer();

        return services;
    }

    private static IServiceCollection CreateBlazorServices()
    {
        var services = new ServiceCollection();
        var environment = new TestHostEnvironment();

        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

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

    private static bool IsMvcControllerEndpoint(Endpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null;
    }

    private static bool RequiresPolicy(Endpoint endpoint, string policy)
    {
        return endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Any(data => string.Equals(data.Policy, policy, StringComparison.OrdinalIgnoreCase));
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

    private static RedirectContext<CookieAuthenticationOptions> CreateRedirectContext(
        IServiceProvider services,
        CookieAuthenticationOptions options,
        string path,
        string redirectUri)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.Request.Path = path;

        return new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler)),
            options,
            new AuthenticationProperties(),
            redirectUri);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "LuminaPath", "LuminaPath.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the LuminaPath repository root.");
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "LuminaPath.Tests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LuminaPath.Tests");
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LuminaPath.Tests", "wwwroot");
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
