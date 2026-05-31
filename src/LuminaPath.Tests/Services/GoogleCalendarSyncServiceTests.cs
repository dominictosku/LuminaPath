using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Test.Utilities;

namespace LuminaPath.Tests.Services;

public class GoogleCalendarSyncServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SyncAsync_PushesLibraryReleasesAndDatedOpenQuests_AndDeletesStale()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        int futureGameId, questId;

        await using (var db = new LuminaPathDbContext(options))
        {
            var futureGame = new Game { Name = "Future Game", Description = "x", ReleaseDate = Now.Date.AddDays(10) };
            var pastGame = new Game { Name = "Old Game", Description = "x", ReleaseDate = Now.Date.AddDays(-10) };
            var otherUserGame = new Game { Name = "Not Mine", Description = "x", ReleaseDate = Now.Date.AddDays(5) };
            db.Games.AddRange(futureGame, pastGame, otherUserGame);

            db.MyGames.AddRange(
                new MyGame { Game = futureGame, LuminaUserId = "user-1", Status = GameStatus.Planned, Priority = 1 },
                new MyGame { Game = pastGame, LuminaUserId = "user-1", Status = GameStatus.Planned, Priority = 1 },
                new MyGame { Game = otherUserGame, LuminaUserId = "user-2", Status = GameStatus.Planned, Priority = 1 });

            db.Quests.AddRange(
                new Quest { LuminaUserId = "user-1", Title = "Dated open", DueDate = Now.Date.AddDays(3), Completed = false },
                new Quest { LuminaUserId = "user-1", Title = "Dated done", DueDate = Now.Date.AddDays(3), Completed = true },
                new Quest { LuminaUserId = "user-1", Title = "No date", Completed = false },
                new Quest { LuminaUserId = "user-2", Title = "Someone else", DueDate = Now.Date.AddDays(3), Completed = false });

            db.CalendarIntegrations.Add(new CalendarIntegration
            {
                LuminaUserId = "user-1",
                Provider = "google",
                EncryptedRefreshToken = "refresh",
                ConnectedAt = Now.AddDays(-1),
            });

            await db.SaveChangesAsync();
            futureGameId = futureGame.Id;
            questId = db.Quests.Single(q => q.Title == "Dated open").Id;
        }

        var calendar = new FakeCalendarApi
        {
            // A leftover event for a game/quest no longer in the desired set.
            ExistingIds = { "lprel999999", "lpquest888888" },
        };
        var service = NewService(options, calendar);

        var result = await service.SyncAsync("user-1", CancellationToken.None);

        Assert.Equal(1, result.ReleaseEvents);
        Assert.Equal(1, result.QuestEvents);
        Assert.Equal(2, result.Deleted);

        Assert.Contains(calendar.Upserts, e => e.Id == $"lprel{futureGameId}");
        Assert.Contains(calendar.Upserts, e => e.Id == $"lpquest{questId}");
        Assert.Equal(2, calendar.Upserts.Count);
        Assert.Equivalent(new[] { "lprel999999", "lpquest888888" }, calendar.Deletes);

        var release = calendar.Upserts.Single(e => e.Id == $"lprel{futureGameId}");
        Assert.Equal(new DateOnly(2026, 6, 11), release.Date);
        Assert.Contains("Future Game", release.Summary);
    }

    [Fact]
    public async Task SyncAsync_StoresResolvedCalendarIdAndLastSyncedAt()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        await using (var db = new LuminaPathDbContext(options))
        {
            db.CalendarIntegrations.Add(new CalendarIntegration
            {
                LuminaUserId = "user-1",
                Provider = "google",
                EncryptedRefreshToken = "refresh",
                ConnectedAt = Now.AddDays(-1),
            });
            await db.SaveChangesAsync();
        }

        var calendar = new FakeCalendarApi { EnsureCalendarId = "lumina-cal-id" };
        var service = NewService(options, calendar);

        await service.SyncAsync("user-1", CancellationToken.None);

        await using var verify = new LuminaPathDbContext(options);
        var link = await verify.CalendarIntegrations.SingleAsync();
        Assert.Equal("lumina-cal-id", link.CalendarId);
        Assert.Equal(Now, link.LastSyncedAt);
        // First sync passes the stored (null) calendar id through to EnsureCalendar.
        Assert.Null(calendar.EnsuredKnownId);
    }

    [Fact]
    public async Task SyncAsync_WhenNotConnected_Throws()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var service = NewService(options, new FakeCalendarApi());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SyncAsync("user-1", CancellationToken.None));
    }

    private static GoogleCalendarSyncService NewService(
        DbContextOptions<LuminaPathDbContext> options,
        FakeCalendarApi calendar)
        => new(
            new TestDbContextFactory(options),
            new FakeOAuth(),
            calendar,
            new IdentityTokenProtector(),
            Options.Create(new GoogleCalendarOptions { CalendarName = "LuminaPath" }),
            () => Now);

    private sealed class FakeOAuth : IGoogleOAuthClient
    {
        public string BuildAuthorizationUrl(string redirectUri, string state) => "https://auth.example";
        public Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct)
            => Task.FromResult(new GoogleTokenResult("access", "refresh", "user@example.com"));
        public Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
            => Task.FromResult(new GoogleTokenResult("access-token", null, null));
        public Task RevokeAsync(string token, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeCalendarApi : IGoogleCalendarApi
    {
        public string EnsureCalendarId { get; set; } = "cal-1";
        public string? EnsuredKnownId { get; private set; }
        public List<string> ExistingIds { get; } = new();
        public List<CalendarEventInput> Upserts { get; } = new();
        public List<string> Deletes { get; } = new();

        public Task<string> EnsureCalendarAsync(string accessToken, string calendarName, string? knownCalendarId, CancellationToken ct)
        {
            EnsuredKnownId = knownCalendarId;
            return Task.FromResult(EnsureCalendarId);
        }

        public Task<IReadOnlyList<string>> ListEventIdsAsync(string accessToken, string calendarId, CancellationToken ct)
            => Task.FromResult((IReadOnlyList<string>)ExistingIds.ToList());

        public Task UpsertEventAsync(string accessToken, string calendarId, CalendarEventInput calendarEvent, CancellationToken ct)
        {
            Upserts.Add(calendarEvent);
            return Task.CompletedTask;
        }

        public Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken ct)
        {
            Deletes.Add(eventId);
            return Task.CompletedTask;
        }
    }

    private sealed class IdentityTokenProtector : ICalendarTokenProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string ciphertext) => ciphertext;
    }
}
