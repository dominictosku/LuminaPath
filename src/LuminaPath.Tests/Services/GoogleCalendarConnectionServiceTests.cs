using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace LuminaPath.Tests.Services;

public class GoogleCalendarConnectionServiceTests
{
    private const string RedirectUri = "https://app.example/api/integrations/google/callback";

    [Fact]
    public void BuildConnectUrl_signsReturnPathInto_State_thatPeekRoundTrips()
    {
        var oauth = new CapturingOAuth();
        var service = NewService(Test.Utilities.DbContext.TestDbContextOptions(), oauth);

        service.BuildConnectUrl("user-1", "/Account/Manage/GoogleCalendar", RedirectUri);

        Assert.Equal("/Account/Manage/GoogleCalendar", service.PeekReturnPath(oauth.LastState));
    }

    [Fact]
    public async Task CompleteConnectionAsync_withMismatchedUser_throwsAndStoresNothing()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var oauth = new CapturingOAuth();
        var service = NewService(options, oauth);
        service.BuildConnectUrl("user-1", "/settings", RedirectUri);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteConnectionAsync("attacker", "code", oauth.LastState!, RedirectUri, CancellationToken.None));

        await using var db = new LuminaPathDbContext(options);
        Assert.Equal(0, await db.CalendarIntegrations.CountAsync());
    }

    [Fact]
    public async Task CompleteConnectionAsync_storesEncryptedRefreshTokenAndEmail()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var oauth = new CapturingOAuth();
        var service = NewService(options, oauth);
        service.BuildConnectUrl("user-1", "/settings", RedirectUri);

        await service.CompleteConnectionAsync("user-1", "code", oauth.LastState!, RedirectUri, CancellationToken.None);

        await using var db = new LuminaPathDbContext(options);
        var link = await db.CalendarIntegrations.SingleAsync();
        Assert.Equal("user-1", link.LuminaUserId);
        Assert.Equal("refresh-token", link.EncryptedRefreshToken); // identity protector in tests
        Assert.Equal("gamer@example.com", link.AccountEmail);
    }

    private static GoogleCalendarConnectionService NewService(
        DbContextOptions<LuminaPathDbContext> options,
        CapturingOAuth oauth)
        => new(
            new TestDbContextFactory(options),
            oauth,
            new IdentityTokenProtector(),
            new EphemeralDataProtectionProvider());

    private sealed class CapturingOAuth : IGoogleOAuthClient
    {
        public string? LastState { get; private set; }

        public string BuildAuthorizationUrl(string redirectUri, string state)
        {
            LastState = state;
            return $"https://accounts.google/o/oauth2?state={state}";
        }

        public Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct)
            => Task.FromResult(new GoogleTokenResult("access", "refresh-token", "gamer@example.com"));

        public Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
            => Task.FromResult(new GoogleTokenResult("access", null, null));

        public Task RevokeAsync(string token, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class IdentityTokenProtector : ICalendarTokenProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string ciphertext) => ciphertext;
    }
}
