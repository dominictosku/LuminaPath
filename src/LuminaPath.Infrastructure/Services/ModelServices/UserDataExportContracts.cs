namespace LuminaPath.Infrastructure.Services.ModelServices;

public sealed record UserDataExportFile(byte[] Content, string FileName);

public sealed record UserDataExportSnapshot(
    int SchemaVersion,
    DateTime GeneratedAt,
    UserExport User,
    LibraryExport Library,
    QuestsExport Quests,
    IReadOnlyList<GamingSessionExport> GamingSessions,
    IReadOnlyList<UserGameAchievementExport> GameAchievements,
    IReadOnlyList<UserDocumentExport> Documents,
    SocialExport Social,
    IReadOnlyList<CalendarIntegrationExport> CalendarIntegrations);

public sealed record UserExport(
    string Id,
    string? UserName,
    string? Email,
    string FullName,
    bool IsActive,
    PsnProfileExport? Psn,
    SteamProfileExport? Steam);

public sealed record PsnProfileExport(
    string OnlineId,
    string AccountId,
    int TrophyLevel,
    int Bronze,
    int Silver,
    int Gold,
    int Platinum);

public sealed record SteamProfileExport(
    string SteamId,
    string PersonaName,
    string ProfileUrl,
    string AvatarUrl);

public sealed record LibraryExport(
    IReadOnlyList<GameLibraryExport> Games,
    IReadOnlyList<AnimeLibraryExport> Animes,
    IReadOnlyList<MovieLibraryExport> Movies,
    IReadOnlyList<SeriesLibraryExport> Series);

public sealed record GameLibraryExport(
    int Id,
    string Status,
    short? Rating,
    int Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    double? TimeSpend,
    int GameId,
    string? PersonalNotes,
    MyGameInfoExport? GameInfo,
    GameMediaExport? Game);

public sealed record AnimeLibraryExport(
    int Id,
    string Status,
    short? Rating,
    int Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    double? TimeSpend,
    int AnimeId,
    int? CurrentWatchTimeMinutes,
    int? CurrentEpisode,
    EpisodeMediaExport? Anime);

public sealed record MovieLibraryExport(
    int Id,
    string Status,
    short? Rating,
    int Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    double? TimeSpend,
    int MovieId,
    int? CurrentWatchTimeMinutes,
    MovieMediaExport? Movie);

public sealed record SeriesLibraryExport(
    int Id,
    string Status,
    short? Rating,
    int Priority,
    DateTime? StartDate,
    DateTime? EndDate,
    double? TimeSpend,
    int SeriesId,
    int? CurrentWatchTimeMinutes,
    int? CurrentEpisode,
    EpisodeMediaExport? Series);

public sealed record MyGameInfoExport(
    double TrackedHours,
    DateTime FirstPlayed,
    DateTime LastPlayed);

public sealed record GameMediaExport(
    MediaExport Media,
    string Platforms,
    int? Playtime,
    int? ParentGameId);

public sealed record EpisodeMediaExport(
    MediaExport Media,
    int? ExpectedWatchTimePerEpisodeMinutes,
    int? EpisodeCount,
    int? ExpectedWatchTimeMinutes,
    int? ParentId);

public sealed record MovieMediaExport(
    MediaExport Media,
    int? ExpectedWatchTimeMinutes);

public sealed record MediaExport(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Genres,
    DateTime? ReleaseDate,
    string Source,
    DocumentExport? Image,
    IReadOnlyList<ExternalIdExport> ExternalIds);

public sealed record ExternalIdExport(string Provider, string ExternalId);

public sealed record DocumentExport(
    int Id,
    string? Name,
    string? StorageName,
    string? Description,
    string? Path,
    string? ContentType,
    string DocumentType,
    string Url);

public sealed record QuestsExport(
    QuestProfileExport? Profile,
    IReadOnlyList<QuestFolderExport> Folders,
    IReadOnlyList<QuestSkillExport> Skills,
    IReadOnlyList<QuestItemExport> Items);

public sealed record QuestProfileExport(
    int Id,
    int TotalXp,
    int CurrentStreakDays,
    int LongestStreakDays,
    DateOnly? LastCompletionDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<QuestAchievementExport> Achievements);

public sealed record QuestAchievementExport(
    int Id,
    string Code,
    DateTime UnlockedAt);

public sealed record QuestFolderExport(
    int Id,
    string Name,
    string Emoji,
    string? Color,
    string? SectionName,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record QuestSkillExport(
    int Id,
    string Name,
    string Icon,
    string Color,
    int Xp,
    DateTime CreatedAt,
    int SortOrder,
    IReadOnlyList<QuestSkillNodeExport> Nodes);

public sealed record QuestSkillNodeExport(
    int Id,
    string Name,
    bool Unlocked,
    DateTime? UnlockedAt,
    int SortOrder);

public sealed record QuestItemExport(
    int Id,
    string Title,
    string? Notes,
    string Type,
    string Priority,
    string Recurrence,
    DateTime? DueDate,
    DateTime? ScheduledStartAt,
    DateTime? ScheduledEndAt,
    IReadOnlyList<string> Tags,
    int RewardXp,
    bool Completed,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int SortOrder,
    int? MyGameId,
    string? GameName,
    int? SkillId,
    string? SkillName,
    int? QuestFolderId,
    string? QuestFolderName,
    IReadOnlyList<QuestSubtaskExport> Subtasks);

public sealed record QuestSubtaskExport(
    int Id,
    string Title,
    bool Completed,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    int SortOrder);

public sealed record GamingSessionExport(
    int Id,
    int? MyGameId,
    string? GameName,
    DateTime ScheduledAt,
    int DurationMinutes,
    bool Completed,
    DateTime? CompletedAt,
    string? Notes,
    DateTime CreatedAt);

public sealed record UserGameAchievementExport(
    int Id,
    string Provider,
    string SourceAchievementId,
    DateTime? UnlockedAt,
    DateTime SyncedAt,
    GameAchievementExport? Achievement);

public sealed record GameAchievementExport(
    int Id,
    int GameId,
    string? GameName,
    string CanonicalKey,
    string Title,
    string? Description,
    string? IconUrl,
    bool IsHidden,
    DateTime LastSyncedAt,
    string? SteamApiName,
    string? SteamDisplayName,
    int? PsnTrophyId,
    string? PsnGroupId,
    string? PsnTrophyType,
    string? PrimaryProvider);

public sealed record UserDocumentExport(
    int Id,
    string? Name,
    string? StorageName,
    string? Description,
    string? Path,
    string? ContentType,
    string DocumentType,
    string Url,
    string Album);

public sealed record SocialExport(
    IReadOnlyList<FriendshipExport> Friendships,
    IReadOnlyList<DirectMessageExport> DirectMessages);

public sealed record FriendshipExport(
    int Id,
    string RequesterId,
    string AddresseeId,
    string Status,
    DateTime CreatedAt,
    DateTime? RespondedAt);

public sealed record DirectMessageExport(
    int Id,
    string SenderId,
    string RecipientId,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt);

public sealed record CalendarIntegrationExport(
    int Id,
    string Provider,
    string? CalendarId,
    string? AccountEmail,
    DateTime ConnectedAt,
    DateTime? LastSyncedAt);
