using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using Test.Utilities;

namespace Test.Services
{
    public class GamingSessionServiceTests
    {
        private static readonly DateTime FixedNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task CreateAsync_AddsSessionForOwnedGame()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hades", Description = "Roguelike", Playtime = 30 };
                dbContext.Games.Add(game);
                var myGame = new MyGame { Game = game, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;
            }

            var service = NewService(options);

            var result = await service.CreateAsync(userId, new GamingSessionDto
            {
                MyGameId = myGameId,
                ScheduledAt = FixedNow.AddDays(2),
                DurationMinutes = 90,
                Notes = "  Boss attempt  "
            });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var saved = await assertContext.GamingSessions.SingleAsync();
            Assert.Equal(userId, saved.LuminaUserId);
            Assert.Equal(myGameId, saved.MyGameId);
            Assert.Equal(90, saved.DurationMinutes);
            Assert.Equal("Boss attempt", saved.Notes);
        }

        [Fact]
        public async Task CreateAsync_RejectsSessionForOtherUsersGame()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            int otherUsersMyGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser("user-1"), NewUser("user-2"));
                var game = new Game { Name = "Hades", Description = "x" };
                dbContext.Games.Add(game);
                var otherGame = new MyGame { Game = game, LuminaUserId = "user-2", Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(otherGame);
                await dbContext.SaveChangesAsync();
                otherUsersMyGameId = otherGame.Id;
            }

            var service = NewService(options);

            var result = await service.CreateAsync("user-1", new GamingSessionDto
            {
                MyGameId = otherUsersMyGameId,
                ScheduledAt = FixedNow.AddDays(1),
                DurationMinutes = 60
            });

            Assert.False(result.IsSuccess);
            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(0, await assertContext.GamingSessions.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_RejectsZeroOrNegativeDuration()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser("user-1"));
                await dbContext.SaveChangesAsync();
            }

            var service = NewService(options);
            var result = await service.CreateAsync("user-1", new GamingSessionDto
            {
                ScheduledAt = FixedNow.AddDays(1),
                DurationMinutes = 0
            });

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task UpdateAsync_PreservesCompletedAtWhenAlreadyComplete()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int sessionId;
            var originalCompletedAt = FixedNow.AddDays(-1);

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var session = new GamingSession
                {
                    LuminaUserId = userId,
                    ScheduledAt = FixedNow.AddDays(-2),
                    DurationMinutes = 60,
                    Completed = true,
                    CompletedAt = originalCompletedAt
                };
                dbContext.GamingSessions.Add(session);
                await dbContext.SaveChangesAsync();
                sessionId = session.Id;
            }

            var service = NewService(options);
            var result = await service.UpdateAsync(userId, sessionId, new GamingSessionDto
            {
                Id = sessionId,
                ScheduledAt = FixedNow.AddDays(-2),
                DurationMinutes = 75,
                Completed = true
            });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var saved = await assertContext.GamingSessions.SingleAsync();
            Assert.Equal(75, saved.DurationMinutes);
            Assert.Equal(originalCompletedAt, saved.CompletedAt);
        }

        [Fact]
        public async Task UpdateAsync_StampsCompletedAtWhenTransitioningToComplete()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int sessionId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var session = new GamingSession
                {
                    LuminaUserId = userId,
                    ScheduledAt = FixedNow.AddDays(-1),
                    DurationMinutes = 60,
                    Completed = false
                };
                dbContext.GamingSessions.Add(session);
                await dbContext.SaveChangesAsync();
                sessionId = session.Id;
            }

            var service = NewService(options);
            var result = await service.UpdateAsync(userId, sessionId, new GamingSessionDto
            {
                Id = sessionId,
                ScheduledAt = FixedNow.AddDays(-1),
                DurationMinutes = 60,
                Completed = true
            });

            Assert.True(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            var saved = await assertContext.GamingSessions.SingleAsync();
            Assert.True(saved.Completed);
            Assert.Equal(FixedNow, saved.CompletedAt);
        }

        [Fact]
        public async Task DeleteAsync_OnlyDeletesCallersOwnSession()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            int otherSessionId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser("user-1"), NewUser("user-2"));
                var otherSession = new GamingSession
                {
                    LuminaUserId = "user-2",
                    ScheduledAt = FixedNow.AddDays(1),
                    DurationMinutes = 60
                };
                dbContext.GamingSessions.Add(otherSession);
                await dbContext.SaveChangesAsync();
                otherSessionId = otherSession.Id;
            }

            var service = NewService(options);
            var result = await service.DeleteAsync("user-1", otherSessionId);

            Assert.False(result.IsSuccess);

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Equal(1, await assertContext.GamingSessions.CountAsync());
        }

        [Fact]
        public async Task GetForUserAsync_FiltersByGameAndDateRange()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hades", Description = "x" };
                dbContext.Games.Add(game);
                var myGame = new MyGame { Game = game, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1 };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;

                dbContext.GamingSessions.AddRange(
                    new GamingSession { LuminaUserId = userId, MyGameId = myGameId, ScheduledAt = FixedNow.AddDays(-5), DurationMinutes = 60 },
                    new GamingSession { LuminaUserId = userId, MyGameId = myGameId, ScheduledAt = FixedNow.AddDays(2), DurationMinutes = 90 },
                    new GamingSession { LuminaUserId = userId, MyGameId = null, ScheduledAt = FixedNow.AddDays(3), DurationMinutes = 30 }
                );
                await dbContext.SaveChangesAsync();
            }

            var service = NewService(options);
            var ranged = await service.GetForUserAsync(userId, myGameId, from: FixedNow, to: FixedNow.AddDays(7));

            var single = Assert.Single(ranged);
            Assert.Equal(90, single.DurationMinutes);
            Assert.Equal("Hades", single.GameName);
        }

        [Fact]
        public async Task BuildForecast_ReturnsZeroRemaining_WhenAlreadyOverPlaytime()
        {
            var myGame = NewMyGameWithPlaytime(estimateHours: 10, played: 12, tracked: 0);
            var forecast = GamingSessionService.BuildForecast(myGame, [], FixedNow);

            Assert.Equal(0, forecast.RemainingHours);
            Assert.Equal(FixedNow, forecast.ProjectedCompletionDate);
            Assert.Equal(0, forecast.SessionsToCompletion);
        }

        [Fact]
        public async Task BuildForecast_PicksFirstSessionThatCoversRemaining()
        {
            // 5h remaining; sessions of 2h, 2h, 3h => third session completes (cumulative 7h).
            var myGame = NewMyGameWithPlaytime(estimateHours: 10, played: 5, tracked: 0);
            var sessions = new List<GamingSession>
            {
                NewSession(FixedNow.AddDays(1), 120),
                NewSession(FixedNow.AddDays(3), 120),
                NewSession(FixedNow.AddDays(5), 180),
                NewSession(FixedNow.AddDays(7), 60)
            };

            var forecast = GamingSessionService.BuildForecast(myGame, sessions, FixedNow);

            Assert.Equal(3, forecast.SessionsToCompletion);
            Assert.Equal(FixedNow.AddDays(5).AddMinutes(180), forecast.ProjectedCompletionDate);
            Assert.Equal(0, forecast.AdditionalHoursNeeded);
        }

        [Fact]
        public async Task BuildForecast_FallsBackToWeeklyPace_WhenSchedulIsShort()
        {
            // 20h remaining; 8h scheduled across 4 weeks => weekly = 2h => 6 more weeks needed for the missing 12h.
            var myGame = NewMyGameWithPlaytime(estimateHours: 30, played: 10, tracked: 0);
            var sessions = new List<GamingSession>
            {
                NewSession(FixedNow.AddDays(2), 120),
                NewSession(FixedNow.AddDays(9), 120),
                NewSession(FixedNow.AddDays(16), 120),
                NewSession(FixedNow.AddDays(23), 120)
            };

            var forecast = GamingSessionService.BuildForecast(myGame, sessions, FixedNow);

            Assert.Null(forecast.ProjectedCompletionDate);
            Assert.Null(forecast.SessionsToCompletion);
            Assert.Equal(8, forecast.ScheduledHours);
            Assert.Equal(2, forecast.WeeklyHours);
            Assert.Equal(12, forecast.AdditionalHoursNeeded);
            Assert.Equal(6, forecast.WeeksAtCurrentPace);
        }

        [Fact]
        public async Task BuildForecast_SkipsCompletionMath_WhenNoPlaytimeEstimate()
        {
            var myGame = NewMyGameWithPlaytime(estimateHours: null, played: 4, tracked: 0);
            var sessions = new List<GamingSession>
            {
                NewSession(FixedNow.AddDays(1), 120)
            };

            var forecast = GamingSessionService.BuildForecast(myGame, sessions, FixedNow);

            Assert.Null(forecast.PlaytimeEstimateHours);
            Assert.Null(forecast.RemainingHours);
            Assert.Null(forecast.SessionsToCompletion);
            Assert.Null(forecast.ProjectedCompletionDate);
            Assert.Equal(2, forecast.ScheduledHours);
        }

        [Fact]
        public async Task GetForecastAsync_ReturnsNullForUnownedGame()
        {
            var options = Utilities.DbContext.TestDbContextOptions();

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.AddRange(NewUser("user-1"), NewUser("user-2"));
                var game = new Game { Name = "Hades", Description = "x", Playtime = 20 };
                dbContext.Games.Add(game);
                dbContext.MyGames.Add(new MyGame { Game = game, LuminaUserId = "user-2", Status = GameStatus.Playing, Priority = 1 });
                await dbContext.SaveChangesAsync();
            }

            var service = NewService(options);
            var forecast = await service.GetForecastAsync("user-1", myGameId: 1);

            Assert.Null(forecast);
        }

        [Fact]
        public async Task GetForecastAsync_OnlyConsidersFutureUncompletedSessionsForOwner()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            const string userId = "user-1";
            int myGameId;

            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.Users.Add(NewUser(userId));
                var game = new Game { Name = "Hades", Description = "x", Playtime = 20 };
                dbContext.Games.Add(game);
                var myGame = new MyGame { Game = game, LuminaUserId = userId, Status = GameStatus.Playing, Priority = 1, TimeSpend = 5 };
                dbContext.MyGames.Add(myGame);
                await dbContext.SaveChangesAsync();
                myGameId = myGame.Id;

                dbContext.GamingSessions.AddRange(
                    // Past session — should be ignored.
                    new GamingSession { LuminaUserId = userId, MyGameId = myGameId, ScheduledAt = FixedNow.AddDays(-3), DurationMinutes = 600 },
                    // Future but completed — should be ignored.
                    new GamingSession { LuminaUserId = userId, MyGameId = myGameId, ScheduledAt = FixedNow.AddDays(1), DurationMinutes = 240, Completed = true, CompletedAt = FixedNow },
                    // Counts: 2h, 4h, 6h, 6h => cumulative 2,6,12,18 => 15 needed => session 4.
                    NewSession(FixedNow.AddDays(2), 120, userId, myGameId),
                    NewSession(FixedNow.AddDays(4), 240, userId, myGameId),
                    NewSession(FixedNow.AddDays(6), 360, userId, myGameId),
                    NewSession(FixedNow.AddDays(8), 360, userId, myGameId)
                );
                await dbContext.SaveChangesAsync();
            }

            var service = NewService(options);
            var forecast = await service.GetForecastAsync(userId, myGameId);

            Assert.NotNull(forecast);
            Assert.Equal(myGameId, forecast.MyGameId);
            Assert.Equal(15, forecast.RemainingHours);
            Assert.Equal(4, forecast.SessionsToCompletion);
            Assert.Equal(FixedNow.AddDays(8).AddMinutes(360), forecast.ProjectedCompletionDate);
        }

        private static GamingSessionService NewService(DbContextOptions<LuminaPathDbContext> options)
        {
            return new GamingSessionService(new TestDbContextFactory(options), () => FixedNow);
        }

        private static MyGame NewMyGameWithPlaytime(int? estimateHours, double played, double tracked)
        {
            return new MyGame
            {
                Id = 1,
                LuminaUserId = "user-1",
                TimeSpend = played,
                Game = new Game { Id = 1, Name = "Hades", Description = "x", Playtime = estimateHours },
                MyGameInfo = tracked > 0 ? new MyGameInfo { TrackedHours = tracked } : null
            };
        }

        private static GamingSession NewSession(DateTime scheduledAt, int minutes, string? userId = null, int? myGameId = null)
        {
            return new GamingSession
            {
                LuminaUserId = userId ?? "user-1",
                MyGameId = myGameId,
                ScheduledAt = scheduledAt,
                DurationMinutes = minutes
            };
        }

        private static LuminaUser NewUser(string id) => new()
        {
            Id = id,
            UserName = $"{id}@example.test",
            Email = $"{id}@example.test"
        };
    }
}
