namespace LuminaPath.Core.Dtos
{
    public class GamingSessionDto
    {
        public int Id { get; set; }
        public int? MyGameId { get; set; }
        public string? GameName { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GameForecastDto
    {
        public int MyGameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public int? PlaytimeEstimateHours { get; set; }
        public double PlayedHours { get; set; }
        public double? RemainingHours { get; set; }
        public double ScheduledHours { get; set; }
        public int UpcomingSessionCount { get; set; }
        public int? SessionsToCompletion { get; set; }
        public DateTime? ProjectedCompletionDate { get; set; }
        public double WeeklyHours { get; set; }
        public double AdditionalHoursNeeded { get; set; }
        public int? WeeksAtCurrentPace { get; set; }
    }
}
