using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class GamingSessionService
    {
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly Func<DateTime> _utcNow;

        public GamingSessionService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
            : this(dbContextFactory, () => DateTime.UtcNow)
        {
        }

        // Test-only seam for deterministic forecasts.
        public GamingSessionService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, Func<DateTime> utcNow)
        {
            _dbContextFactory = dbContextFactory;
            _utcNow = utcNow;
        }

        public async Task<List<GamingSessionDto>> GetForUserAsync(
            string userId,
            int? myGameId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            IQueryable<GamingSession> query = dbContext.GamingSessions
                .AsNoTracking()
                .Include(session => session.MyGame!)
                    .ThenInclude(myGame => myGame.Game)
                .Where(session => session.LuminaUserId == userId);

            if (myGameId.HasValue)
            {
                query = query.Where(session => session.MyGameId == myGameId);
            }
            if (from.HasValue)
            {
                query = query.Where(session => session.ScheduledAt >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(session => session.ScheduledAt < to.Value);
            }

            return await query
                .OrderBy(session => session.ScheduledAt)
                .Select(session => MapToDto(session))
                .ToListAsync();
        }

        public async Task<Result<GamingSessionDto, FailedResult>> CreateAsync(string userId, GamingSessionDto dto)
        {
            if (dto.DurationMinutes <= 0)
            {
                return new FailedResult("Duration must be greater than zero.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            if (dto.MyGameId.HasValue && !await UserOwnsMyGame(dbContext, userId, dto.MyGameId.Value))
            {
                return new FailedResult("Selected game is not in your library.");
            }

            var session = new GamingSession
            {
                LuminaUserId = userId,
                MyGameId = dto.MyGameId,
                ScheduledAt = dto.ScheduledAt,
                DurationMinutes = dto.DurationMinutes,
                Completed = dto.Completed,
                CompletedAt = dto.Completed ? dto.CompletedAt ?? _utcNow() : null,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedAt = _utcNow()
            };

            await dbContext.GamingSessions.AddAsync(session);
            await dbContext.SaveChangesAsync();

            return await GetSingleDto(dbContext, session.Id);
        }

        public async Task<Result<GamingSessionDto, FailedResult>> UpdateAsync(string userId, int id, GamingSessionDto dto)
        {
            if (dto.DurationMinutes <= 0)
            {
                return new FailedResult("Duration must be greater than zero.");
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var session = await dbContext.GamingSessions
                .FirstOrDefaultAsync(s => s.Id == id && s.LuminaUserId == userId);

            if (session == null)
            {
                return new FailedResult("Session not found.");
            }

            if (dto.MyGameId.HasValue && !await UserOwnsMyGame(dbContext, userId, dto.MyGameId.Value))
            {
                return new FailedResult("Selected game is not in your library.");
            }

            session.MyGameId = dto.MyGameId;
            session.ScheduledAt = dto.ScheduledAt;
            session.DurationMinutes = dto.DurationMinutes;
            session.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            if (dto.Completed && !session.Completed)
            {
                session.Completed = true;
                session.CompletedAt = dto.CompletedAt ?? _utcNow();
            }
            else if (!dto.Completed && session.Completed)
            {
                session.Completed = false;
                session.CompletedAt = null;
            }
            else if (dto.Completed && dto.CompletedAt.HasValue)
            {
                session.CompletedAt = dto.CompletedAt;
            }

            await dbContext.SaveChangesAsync();
            return await GetSingleDto(dbContext, id);
        }

        public async Task<Result<int, FailedResult>> DeleteAsync(string userId, int id)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var session = await dbContext.GamingSessions
                .FirstOrDefaultAsync(s => s.Id == id && s.LuminaUserId == userId);

            if (session == null)
            {
                return new FailedResult("Session not found.");
            }

            dbContext.GamingSessions.Remove(session);
            await dbContext.SaveChangesAsync();
            return id;
        }

        public async Task<GameForecastDto?> GetForecastAsync(string userId, int myGameId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var myGame = await dbContext.MyGames
                .AsNoTracking()
                .Include(myGame => myGame.Game)
                .Include(myGame => myGame.MyGameInfo)
                .FirstOrDefaultAsync(myGame => myGame.Id == myGameId && myGame.LuminaUserId == userId);

            if (myGame?.Game == null)
            {
                return null;
            }

            var now = _utcNow();
            var sessions = await dbContext.GamingSessions
                .AsNoTracking()
                .Where(session => session.LuminaUserId == userId
                    && session.MyGameId == myGameId
                    && !session.Completed
                    && session.ScheduledAt >= now)
                .OrderBy(session => session.ScheduledAt)
                .ToListAsync();

            return BuildForecast(myGame, sessions, now);
        }

        public static GameForecastDto BuildForecast(MyGame myGame, List<GamingSession> upcomingSessions, DateTime now)
        {
            var played = (myGame.TimeSpend ?? 0) + (myGame.MyGameInfo?.TrackedHours ?? 0);
            int? estimate = myGame.Game?.Playtime;
            double? remaining = estimate.HasValue
                ? Math.Max(0, estimate.Value - played)
                : null;

            var forecast = new GameForecastDto
            {
                MyGameId = myGame.Id,
                GameName = myGame.Game?.Name ?? string.Empty,
                PlaytimeEstimateHours = estimate,
                PlayedHours = played,
                RemainingHours = remaining,
                UpcomingSessionCount = upcomingSessions.Count
            };

            double scheduledHours = upcomingSessions.Sum(session => session.DurationMinutes / 60.0);
            forecast.ScheduledHours = scheduledHours;

            // Weekly average over the next 4 weeks (28 days) of the schedule.
            var horizon = now.AddDays(28);
            double next4WeeksHours = upcomingSessions
                .Where(session => session.ScheduledAt < horizon)
                .Sum(session => session.DurationMinutes / 60.0);
            forecast.WeeklyHours = Math.Round(next4WeeksHours / 4.0, 2);

            if (!remaining.HasValue || remaining.Value <= 0)
            {
                if (remaining.HasValue && remaining.Value <= 0)
                {
                    forecast.AdditionalHoursNeeded = 0;
                    forecast.SessionsToCompletion = 0;
                    forecast.ProjectedCompletionDate = now;
                }
                return forecast;
            }

            double cumulative = 0;
            for (int index = 0; index < upcomingSessions.Count; index++)
            {
                var session = upcomingSessions[index];
                cumulative += session.DurationMinutes / 60.0;
                if (cumulative >= remaining.Value)
                {
                    forecast.SessionsToCompletion = index + 1;
                    forecast.ProjectedCompletionDate = session.ScheduledAt.AddMinutes(session.DurationMinutes);
                    forecast.AdditionalHoursNeeded = 0;
                    return forecast;
                }
            }

            // Schedule is short — extrapolate at the weekly pace.
            forecast.AdditionalHoursNeeded = Math.Max(0, Math.Round(remaining.Value - scheduledHours, 2));
            if (forecast.WeeklyHours > 0)
            {
                forecast.WeeksAtCurrentPace = (int)Math.Ceiling(forecast.AdditionalHoursNeeded / forecast.WeeklyHours);
            }
            return forecast;
        }

        private static async Task<bool> UserOwnsMyGame(LuminaPathDbContext dbContext, string userId, int myGameId)
        {
            return await dbContext.MyGames
                .AsNoTracking()
                .AnyAsync(myGame => myGame.Id == myGameId && myGame.LuminaUserId == userId);
        }

        private static async Task<GamingSessionDto> GetSingleDto(LuminaPathDbContext dbContext, int id)
        {
            var session = await dbContext.GamingSessions
                .AsNoTracking()
                .Include(session => session.MyGame!)
                    .ThenInclude(myGame => myGame.Game)
                .FirstAsync(session => session.Id == id);
            return MapToDto(session);
        }

        private static GamingSessionDto MapToDto(GamingSession session)
        {
            return new GamingSessionDto
            {
                Id = session.Id,
                MyGameId = session.MyGameId,
                GameName = session.MyGame?.Game?.Name,
                ScheduledAt = session.ScheduledAt,
                DurationMinutes = session.DurationMinutes,
                Completed = session.Completed,
                CompletedAt = session.CompletedAt,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt
            };
        }
    }
}
