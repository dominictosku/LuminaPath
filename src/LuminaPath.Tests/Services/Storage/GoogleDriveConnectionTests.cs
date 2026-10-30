using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace LuminaPath.Tests.Services.Storage;

public sealed class GoogleDriveConnectionTests
{
    [Fact]
    public async Task BrowserFlow_UsesPkceAndPersistsEncryptedGrantAcrossRestart()
    {
        using var f = new Fixture();
        var query = QueryHelpers.ParseQuery(new Uri(f.Service.BuildConnectUrl("admin")).Query);
        Assert.Equal(GoogleDriveConnectionService.Scope, query["scope"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.DoesNotContain("client_secret", query.Keys);
        await f.Service.CompleteAsync("admin", query["state"], "authorization-code", null, default);
        var posted = QueryHelpers.ParseQuery(f.Http.TokenRequest!);
        Assert.Equal("authorization-code", posted["code"]);
        Assert.Equal(f.Options.RedirectUri, posted["redirect_uri"]);
        Assert.Equal(query["code_challenge"], WebEncoders.Base64UrlEncode(
            SHA256.HashData(Encoding.ASCII.GetBytes(posted["code_verifier"].ToString()))));
        var saved = f.NewStore().Read();
        Assert.Equal("private-refresh-token", saved!.RefreshToken);
        Assert.Equal("account-one", saved.AccountId);
        var path = Path.Combine(f.Directory, "connection.protected");
        Assert.DoesNotContain("private-refresh-token", await File.ReadAllTextAsync(path));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CompleteAsync("admin", query["state"], "code", null, default));
        Assert.Equal(1, f.Http.TokenRequests);
    }

    [Theory]
    [InlineData("different-admin", false, null)]
    [InlineData("admin", true, null)]
    [InlineData("admin", false, "access_denied")]
    public async Task WrongUserTamperedStateAndDenial_DoNotExchangeCode(string user, bool tamper, string? error)
    {
        using var f = new Fixture();
        var state = f.Start();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.CompleteAsync(user, tamper ? state + "x" : state, "code", error, default));
        Assert.Equal(0, f.Http.TokenRequests);
        Assert.Null(f.Store.Read());
    }

    [Fact]
    public async Task PendingState_DoesNotSurviveProcessRestart()
    {
        using var f = new Fixture();
        var state = f.Start();
        using var restarted = new GoogleDriveConnectionService(new HttpClient(f.Http), Options.Create(f.Options), f.Store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => restarted.CompleteAsync("admin", state, "code", null, default));
        Assert.Equal(0, f.Http.TokenRequests);
    }

    [Theory]
    [InlineData("", "private-refresh-token")]
    [InlineData(GoogleDriveConnectionService.Scope, "")]
    public async Task MissingScopeOrOfflineGrant_DoesNotPersist(string scope, string refresh)
    {
        using var f = new Fixture();
        f.Http.Scope = scope;
        f.Http.Refresh = refresh;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CompleteAsync("admin", f.Start(), "code", null, default));
        Assert.Null(f.Store.Read());
    }

    [Fact]
    public async Task Reconnection_CannotReplaceStorageAccount_AndPreservesOldGrantOnFailure()
    {
        using var f = new Fixture();
        await f.Service.CompleteAsync("admin", f.Start(), "code", null, default);
        f.Http.Account = "other-account";
        f.Http.Refresh = "replacement-token";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.CompleteAsync("admin", f.Start(), "code", null, default));
        Assert.Equal("private-refresh-token", f.Store.Read()!.RefreshToken);
        f.Http.Account = "account-one";
        await f.Service.CompleteAsync("admin", f.Start(), "code", null, default);
        Assert.Equal("replacement-token", f.Store.Read()!.RefreshToken);
    }

    [Fact]
    public async Task SavedConnection_RejectsChangedOAuthClient()
    {
        using var f = new Fixture();
        await f.Service.CompleteAsync("admin", f.Start(), "code", null, default);
        f.Options.ClientId = "another-client";
        Assert.Throws<InvalidOperationException>(() => f.NewStore().Read());
    }

    [Fact]
    public void ExternalRefreshToken_CannotBeSilentlyOverriddenByBrowser()
    {
        using var f = new Fixture();
        f.Options.RefreshToken = "external-token";
        Assert.Throws<InvalidOperationException>(() => f.Start());
    }

    [Fact]
    public async Task DriveClientCreatedBeforeAuthorization_UsesSavedGrantWithoutRestart()
    {
        using var f = new Fixture();
        using var client = new GoogleDriveClient(new HttpClient(f.Http), Options.Create(f.Options), f.Store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListAsync("uploads"));
        await f.Service.CompleteAsync("admin", f.Start(), "code", null, default);
        Assert.Empty(await client.ListAsync("uploads"));
        Assert.Equal("private-refresh-token", QueryHelpers.ParseQuery(f.Http.TokenRequest!)["refresh_token"]);
    }

    [Fact]
    public async Task ControllerRequiresAdminAndAntiforgery_AndNeverReturnsOAuthErrors()
    {
        using var f = new Fixture();
        var authorization = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(GoogleDriveController), typeof(AuthorizeAttribute))!;
        Assert.Equal("Administrator", authorization.Roles);
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("missing"));
        var controller = new GoogleDriveController(f.Service, antiforgery.Object, NullLogger<GoogleDriveController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin")], "Test"))
            } }
        };
        Assert.IsType<BadRequestObjectResult>(await controller.Connect());
        Assert.Equal(0, f.Http.TokenRequests);
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var redirect = Assert.IsType<RedirectResult>(await controller.Connect());
        var state = QueryHelpers.ParseQuery(new Uri(redirect.Url).Query)["state"].ToString();
        f.Http.Fail = true;
        var failed = Assert.IsType<RedirectResult>(await controller.Callback("secret-code", state, null, default));
        Assert.Equal("/Admin/Overview?tab=storage&drive=error", failed.Url);
        Assert.Null(f.Store.Read());
    }

    private sealed class Fixture : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "drive-oauth-tests-" + Guid.NewGuid());
        public GoogleDriveOptions Options { get; }
        public FakeOAuth Http { get; } = new();
        public GoogleDriveConnectionStore Store { get; }
        public GoogleDriveConnectionService Service { get; }
        public Fixture()
        {
            Options = new GoogleDriveOptions
            {
                ClientId = "web-client", ClientSecret = "secret", ConnectionPath = Directory,
                RedirectUri = "https://vpn.example.com/api/integrations/google-drive/callback", FolderId = "root"
            };
            Store = NewStore();
            Service = new GoogleDriveConnectionService(new HttpClient(Http), Microsoft.Extensions.Options.Options.Create(Options), Store);
        }
        public GoogleDriveConnectionStore NewStore()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());
            return new(Microsoft.Extensions.Options.Options.Create(Options), environment.Object);
        }
        public string Start() => QueryHelpers.ParseQuery(new Uri(Service.BuildConnectUrl("admin")).Query)["state"].ToString();
        public void Dispose()
        {
            Service.Dispose();
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
        }
    }

    private sealed class FakeOAuth : HttpMessageHandler
    {
        public string Scope { get; set; } = GoogleDriveConnectionService.Scope;
        public string Refresh { get; set; } = "private-refresh-token";
        public string Account { get; set; } = "account-one";
        public string? TokenRequest { get; private set; }
        public int TokenRequests { get; private set; }
        public bool Fail { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!;
            if (url.AbsoluteUri == "https://oauth2.googleapis.com/token")
            {
                TokenRequests++;
                TokenRequest = await request.Content!.ReadAsStringAsync(ct);
                if (Fail) return new(HttpStatusCode.BadRequest) { Content = new StringContent("private-refresh-token") };
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new
                { access_token = "private-access-token", refresh_token = Refresh, scope = Scope, expires_in = 3600 }) };
            }
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            if (url.AbsolutePath.EndsWith("/about"))
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { user = new { permissionId = Account } }) };
            if (url.AbsolutePath.EndsWith("/files/root"))
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "root", name = "Root", mimeType = "application/vnd.google-apps.folder" }) };
            if (url.AbsolutePath.EndsWith("/files"))
            {
                var query = QueryHelpers.ParseQuery(url.Query)["q"].ToString();
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new
                {
                    files = query.Contains("appProperties")
                        ? new[] { new { id = "uploads-folder", name = "uploads", mimeType = "application/vnd.google-apps.folder" } } : []
                }) };
            }
            throw new InvalidOperationException("Unexpected request.");
        }
    }
}
