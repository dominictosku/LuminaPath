namespace LuminaPath.Infrastructure.Services.ModelServices
{
    internal static class AchievementCatalog
    {
        public const string FirstQuest = "first_quest";
        public const string TenQuests = "ten_quests";
        public const string HundredQuests = "hundred_quests";
        public const string FirstMainQuest = "first_main_quest";
        public const string DailyDiscipline = "daily_discipline";
        public const string Completionist = "completionist";
        public const string SevenDayStreak = "seven_day_streak";
        public const string ThirtyDayStreak = "thirty_day_streak";

        private static readonly Dictionary<string, AchievementDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [FirstQuest] = new("First Steps", "Complete your first quest.", "footsteps-outline"),
            [TenQuests] = new("Apprentice", "Complete 10 quests.", "ribbon-outline"),
            [HundredQuests] = new("Centurion", "Complete 100 quests.", "trophy-outline"),
            [FirstMainQuest] = new("Main Story", "Complete your first main quest.", "map-outline"),
            [DailyDiscipline] = new("Daily Discipline", "Complete a recurring quest.", "refresh-outline"),
            [Completionist] = new("Completionist", "Finish every subtask on a boss quest.", "checkmark-done-outline"),
            [SevenDayStreak] = new("Week One", "Maintain a 7-day quest streak.", "flame-outline"),
            [ThirtyDayStreak] = new("Unbroken", "Maintain a 30-day quest streak.", "flame")
        };

        public static AchievementDefinition? TryGet(string code)
        {
            return Definitions.TryGetValue(code, out var def) ? def : null;
        }
    }

    internal sealed record AchievementDefinition(string Title, string Description, string Icon);
}
