using System.Globalization;
using System.Text.Json;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.ThirdParty;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public sealed class UserDataExportService
{
    private const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly Func<DateTime> _utcNow;

    public UserDataExportService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        : this(dbContextFactory, () => DateTime.UtcNow)
    {
    }

    public UserDataExportService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, Func<DateTime> utcNow)
    {
        _dbContextFactory = dbContextFactory;
        _utcNow = utcNow;
    }

    public async Task<UserDataExportFile> ExportAsync(string userId, CancellationToken cancellationToken = default)
    {
        var generatedAt = _utcNow().ToUniversalTime();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(user => user.LuminaUserInfo)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        var snapshot = new UserDataExportSnapshot(
            SchemaVersion: CurrentSchemaVersion,
            GeneratedAt: generatedAt,
            User: MapUser(user, userId),
            Library: await ExportLibraryAsync(dbContext, userId, cancellationToken),
            Quests: await ExportQuestsAsync(dbContext, userId, cancellationToken),
            GamingSessions: await ExportGamingSessionsAsync(dbContext, userId, cancellationToken),
            GameAchievements: await ExportGameAchievementsAsync(dbContext, userId, cancellationToken),
            Documents: await ExportDocumentsAsync(dbContext, userId, cancellationToken),
            Social: await ExportSocialAsync(dbContext, userId, cancellationToken),
            CalendarIntegrations: await ExportCalendarIntegrationsAsync(dbContext, userId, cancellationToken));

        var content = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        var fileName = $"LuminaPath-export-{generatedAt.ToString("yyyyMMdd-HHmmss'Z'", CultureInfo.InvariantCulture)}.json";
        return new UserDataExportFile(content, fileName);
    }

    private static UserExport MapUser(Identity.LuminaUser? user, string userId)
    {
        var info = user?.LuminaUserInfo;
        return new UserExport(
            Id: user?.Id ?? userId,
            UserName: user?.UserName,
            Email: user?.Email,
            FullName: user?.FullName ?? string.Empty,
            IsActive: user?.IsActive ?? false,
            Psn: info is null
                ? null
                : new PsnProfileExport(
                    OnlineId: info.PSNOnlineId,
                    AccountId: info.PSNAccountId,
                    TrophyLevel: info.PSNTrophyLevel,
                    Bronze: info.PSNBronze,
                    Silver: info.PSNSilver,
                    Gold: info.PSNGold,
                    Platinum: info.PSNPlatinum),
            Steam: info is null
                ? null
                : new SteamProfileExport(
                    SteamId: info.SteamId,
                    PersonaName: info.SteamPersonaName,
                    ProfileUrl: info.SteamProfileUrl,
                    AvatarUrl: info.SteamAvatarUrl));
    }

    private static async Task<LibraryExport> ExportLibraryAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var games = await dbContext.MyGames
            .AsNoTracking()
            .Include(myGame => myGame.Game)
                .ThenInclude(game => game!.ExternalIds)
            .Include(myGame => myGame.Game)
                .ThenInclude(game => game!.Image)
            .Include(myGame => myGame.MyGameInfo)
            .Where(myGame => myGame.LuminaUserId == userId)
            .OrderBy(myGame => myGame.Game == null ? string.Empty : myGame.Game.Name)
            .ThenBy(myGame => myGame.Id)
            .ToListAsync(cancellationToken);

        var animes = await dbContext.MyAnimes
            .AsNoTracking()
            .Include(myAnime => myAnime.Anime)
                .ThenInclude(anime => anime!.ExternalIds)
            .Include(myAnime => myAnime.Anime)
                .ThenInclude(anime => anime!.Image)
            .Where(myAnime => myAnime.LuminaUserId == userId)
            .OrderBy(myAnime => myAnime.Anime == null ? string.Empty : myAnime.Anime.Name)
            .ThenBy(myAnime => myAnime.Id)
            .ToListAsync(cancellationToken);

        var movies = await dbContext.MyMovies
            .AsNoTracking()
            .Include(myMovie => myMovie.Movie)
                .ThenInclude(movie => movie!.ExternalIds)
            .Include(myMovie => myMovie.Movie)
                .ThenInclude(movie => movie!.Image)
            .Where(myMovie => myMovie.LuminaUserId == userId)
            .OrderBy(myMovie => myMovie.Movie == null ? string.Empty : myMovie.Movie.Name)
            .ThenBy(myMovie => myMovie.Id)
            .ToListAsync(cancellationToken);

        var series = await dbContext.MySeries
            .AsNoTracking()
            .Include(mySeries => mySeries.Series)
                .ThenInclude(series => series!.ExternalIds)
            .Include(mySeries => mySeries.Series)
                .ThenInclude(series => series!.Image)
            .Where(mySeries => mySeries.LuminaUserId == userId)
            .OrderBy(mySeries => mySeries.Series == null ? string.Empty : mySeries.Series.Name)
            .ThenBy(mySeries => mySeries.Id)
            .ToListAsync(cancellationToken);

        return new LibraryExport(
            Games: games.Select(MapGameLibraryEntry).ToList(),
            Animes: animes.Select(MapAnimeLibraryEntry).ToList(),
            Movies: movies.Select(MapMovieLibraryEntry).ToList(),
            Series: series.Select(MapSeriesLibraryEntry).ToList());
    }

    private static async Task<QuestsExport> ExportQuestsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.QuestProfiles
            .AsNoTracking()
            .Include(profile => profile.Achievements)
            .FirstOrDefaultAsync(profile => profile.LuminaUserId == userId, cancellationToken);

        var folders = await dbContext.QuestFolders
            .AsNoTracking()
            .Where(folder => folder.LuminaUserId == userId)
            .OrderBy(folder => folder.SortOrder)
            .ThenBy(folder => folder.Id)
            .ToListAsync(cancellationToken);

        var skills = await dbContext.QuestSkills
            .AsNoTracking()
            .Include(skill => skill.Nodes)
            .Where(skill => skill.LuminaUserId == userId)
            .OrderBy(skill => skill.SortOrder)
            .ThenBy(skill => skill.Id)
            .ToListAsync(cancellationToken);

        var quests = await dbContext.Quests
            .AsNoTracking()
            .Include(quest => quest.Subtasks)
            .Include(quest => quest.MyGame)
                .ThenInclude(myGame => myGame!.Game)
            .Include(quest => quest.Skill)
            .Include(quest => quest.QuestFolder)
            .Where(quest => quest.LuminaUserId == userId)
            .OrderBy(quest => quest.SortOrder)
            .ThenBy(quest => quest.Id)
            .ToListAsync(cancellationToken);

        return new QuestsExport(
            Profile: profile is null ? null : MapQuestProfile(profile),
            Folders: folders.Select(MapQuestFolder).ToList(),
            Skills: skills.Select(MapQuestSkill).ToList(),
            Items: quests.Select(MapQuestItem).ToList());
    }

    private static async Task<IReadOnlyList<GamingSessionExport>> ExportGamingSessionsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.GamingSessions
            .AsNoTracking()
            .Include(session => session.MyGame)
                .ThenInclude(myGame => myGame!.Game)
            .Where(session => session.LuminaUserId == userId)
            .OrderBy(session => session.ScheduledAt)
            .ThenBy(session => session.Id)
            .ToListAsync(cancellationToken);

        return sessions.Select(MapGamingSession).ToList();
    }

    private static async Task<IReadOnlyList<UserGameAchievementExport>> ExportGameAchievementsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var achievements = await dbContext.UserGameAchievements
            .AsNoTracking()
            .Include(userAchievement => userAchievement.GameAchievement)
                .ThenInclude(achievement => achievement!.Game)
            .Where(userAchievement => userAchievement.LuminaUserId == userId)
            .OrderByDescending(userAchievement => userAchievement.UnlockedAt ?? userAchievement.SyncedAt)
            .ThenBy(userAchievement => userAchievement.Id)
            .ToListAsync(cancellationToken);

        return achievements.Select(MapUserGameAchievement).ToList();
    }

    private static async Task<IReadOnlyList<UserDocumentExport>> ExportDocumentsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var documents = await dbContext.UserDocuments
            .AsNoTracking()
            .Where(document => document.UserId == userId)
            .OrderBy(document => document.Album)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(MapUserDocument).ToList();
    }

    private static async Task<SocialExport> ExportSocialAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var friendships = await dbContext.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.RequesterId == userId || friendship.AddresseeId == userId)
            .OrderByDescending(friendship => friendship.CreatedAt)
            .ThenBy(friendship => friendship.Id)
            .ToListAsync(cancellationToken);

        var directMessages = await dbContext.DirectMessages
            .AsNoTracking()
            .Where(message => message.SenderId == userId || message.RecipientId == userId)
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);

        return new SocialExport(
            Friendships: friendships.Select(MapFriendship).ToList(),
            DirectMessages: directMessages.Select(MapDirectMessage).ToList());
    }

    private static async Task<IReadOnlyList<CalendarIntegrationExport>> ExportCalendarIntegrationsAsync(
        LuminaPathDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var integrations = await dbContext.CalendarIntegrations
            .AsNoTracking()
            .Where(integration => integration.LuminaUserId == userId)
            .OrderBy(integration => integration.Provider)
            .ThenBy(integration => integration.Id)
            .ToListAsync(cancellationToken);

        return integrations.Select(MapCalendarIntegration).ToList();
    }

    private static GameLibraryExport MapGameLibraryEntry(MyGame myGame)
    {
        return new GameLibraryExport(
            Id: myGame.Id,
            Status: myGame.Status.ToString(),
            Rating: myGame.Rating,
            Priority: myGame.Priority,
            StartDate: myGame.StartDate,
            EndDate: myGame.EndDate,
            TimeSpend: myGame.TimeSpend,
            GameId: myGame.GameId,
            PersonalNotes: myGame.PersonalNotes,
            GameInfo: myGame.MyGameInfo is null
                ? null
                : new MyGameInfoExport(
                    TrackedHours: myGame.MyGameInfo.TrackedHours,
                    FirstPlayed: myGame.MyGameInfo.FirstPlayed,
                    LastPlayed: myGame.MyGameInfo.LastPlayed),
            Game: myGame.Game is null
                ? null
                : new GameMediaExport(
                    Media: MapMedia(myGame.Game),
                    Platforms: myGame.Game.Platforms.ToString(),
                    Playtime: myGame.Game.Playtime,
                    ParentGameId: myGame.Game.ParentGameId));
    }

    private static AnimeLibraryExport MapAnimeLibraryEntry(MyAnime myAnime)
    {
        return new AnimeLibraryExport(
            Id: myAnime.Id,
            Status: myAnime.Status.ToString(),
            Rating: myAnime.Rating,
            Priority: myAnime.Priority,
            StartDate: myAnime.StartDate,
            EndDate: myAnime.EndDate,
            TimeSpend: myAnime.TimeSpend,
            AnimeId: myAnime.AnimeId,
            CurrentWatchTimeMinutes: myAnime.CurrentWatchTimeMinutes,
            CurrentEpisode: myAnime.CurrentEpisode,
            Anime: myAnime.Anime is null
                ? null
                : new EpisodeMediaExport(
                    Media: MapMedia(myAnime.Anime),
                    ExpectedWatchTimePerEpisodeMinutes: myAnime.Anime.ExpectedWatchTimePerEpisodeMinutes,
                    EpisodeCount: myAnime.Anime.EpisodeCount,
                    ExpectedWatchTimeMinutes: myAnime.Anime.ExpectedWatchTimeMinutes,
                    ParentId: myAnime.Anime.ParentAnimeId));
    }

    private static MovieLibraryExport MapMovieLibraryEntry(MyMovie myMovie)
    {
        return new MovieLibraryExport(
            Id: myMovie.Id,
            Status: myMovie.Status.ToString(),
            Rating: myMovie.Rating,
            Priority: myMovie.Priority,
            StartDate: myMovie.StartDate,
            EndDate: myMovie.EndDate,
            TimeSpend: myMovie.TimeSpend,
            MovieId: myMovie.MovieId,
            CurrentWatchTimeMinutes: myMovie.CurrentWatchTimeMinutes,
            Movie: myMovie.Movie is null
                ? null
                : new MovieMediaExport(
                    Media: MapMedia(myMovie.Movie),
                    ExpectedWatchTimeMinutes: myMovie.Movie.ExpectedWatchTimeMinutes));
    }

    private static SeriesLibraryExport MapSeriesLibraryEntry(MySeries mySeries)
    {
        return new SeriesLibraryExport(
            Id: mySeries.Id,
            Status: mySeries.Status.ToString(),
            Rating: mySeries.Rating,
            Priority: mySeries.Priority,
            StartDate: mySeries.StartDate,
            EndDate: mySeries.EndDate,
            TimeSpend: mySeries.TimeSpend,
            SeriesId: mySeries.SeriesId,
            CurrentWatchTimeMinutes: mySeries.CurrentWatchTimeMinutes,
            CurrentEpisode: mySeries.CurrentEpisode,
            Series: mySeries.Series is null
                ? null
                : new EpisodeMediaExport(
                    Media: MapMedia(mySeries.Series),
                    ExpectedWatchTimePerEpisodeMinutes: mySeries.Series.ExpectedWatchTimePerEpisodeMinutes,
                    EpisodeCount: mySeries.Series.EpisodeCount,
                    ExpectedWatchTimeMinutes: mySeries.Series.ExpectedWatchTimeMinutes,
                    ParentId: mySeries.Series.ParentSeriesId));
    }

    private static MediaExport MapMedia(Media media)
    {
        return new MediaExport(
            Id: media.Id,
            Name: media.Name,
            Description: media.Description,
            Genres: media.Genres.ToList(),
            ReleaseDate: media.ReleaseDate,
            Source: media.Source,
            Image: media.Image is null ? null : MapDocument(media.Image),
            ExternalIds: media.ExternalIds
                .OrderBy(externalId => externalId.Provider)
                .ThenBy(externalId => externalId.ExternalId)
                .Select(MapExternalId)
                .ToList());
    }

    private static ExternalIdExport MapExternalId(MediaExternalId externalId)
    {
        return new ExternalIdExport(
            Provider: externalId.Provider.ToString(),
            ExternalId: externalId.ExternalId);
    }

    private static DocumentExport MapDocument(Document document)
    {
        return new DocumentExport(
            Id: document.Id,
            Name: document.Name,
            StorageName: document.StorageName,
            Description: document.Description,
            Path: document.Path,
            ContentType: document.ContentType,
            DocumentType: document.DocumentType.ToString(),
            Url: document.Url);
    }

    private static QuestProfileExport MapQuestProfile(QuestProfile profile)
    {
        return new QuestProfileExport(
            Id: profile.Id,
            TotalXp: profile.TotalXp,
            CurrentStreakDays: profile.CurrentStreakDays,
            LongestStreakDays: profile.LongestStreakDays,
            LastCompletionDate: profile.LastCompletionDate,
            CreatedAt: profile.CreatedAt,
            UpdatedAt: profile.UpdatedAt,
            Achievements: profile.Achievements
                .OrderByDescending(achievement => achievement.UnlockedAt)
                .ThenBy(achievement => achievement.Id)
                .Select(MapQuestAchievement)
                .ToList());
    }

    private static QuestAchievementExport MapQuestAchievement(Achievement achievement)
    {
        return new QuestAchievementExport(
            Id: achievement.Id,
            Code: achievement.Code,
            UnlockedAt: achievement.UnlockedAt);
    }

    private static QuestFolderExport MapQuestFolder(QuestFolder folder)
    {
        return new QuestFolderExport(
            Id: folder.Id,
            Name: folder.Name,
            Emoji: folder.Emoji,
            Color: folder.Color,
            SectionName: folder.SectionName,
            SortOrder: folder.SortOrder,
            CreatedAt: folder.CreatedAt,
            UpdatedAt: folder.UpdatedAt);
    }

    private static QuestSkillExport MapQuestSkill(QuestSkill skill)
    {
        return new QuestSkillExport(
            Id: skill.Id,
            Name: skill.Name,
            Icon: skill.Icon,
            Color: skill.Color,
            Xp: skill.Xp,
            CreatedAt: skill.CreatedAt,
            SortOrder: skill.SortOrder,
            Nodes: skill.Nodes
                .OrderBy(node => node.SortOrder)
                .ThenBy(node => node.Id)
                .Select(MapQuestSkillNode)
                .ToList());
    }

    private static QuestSkillNodeExport MapQuestSkillNode(QuestSkillNode node)
    {
        return new QuestSkillNodeExport(
            Id: node.Id,
            Name: node.Name,
            Unlocked: node.Unlocked,
            UnlockedAt: node.UnlockedAt,
            SortOrder: node.SortOrder);
    }

    private static QuestItemExport MapQuestItem(Quest quest)
    {
        return new QuestItemExport(
            Id: quest.Id,
            Title: quest.Title,
            Notes: quest.Notes,
            Type: quest.Type.ToString(),
            Priority: quest.Priority.ToString(),
            Recurrence: quest.Recurrence.ToString(),
            DueDate: quest.DueDate,
            Tags: quest.Tags.ToList(),
            RewardXp: quest.RewardXp,
            Completed: quest.Completed,
            CompletedAt: quest.CompletedAt,
            CreatedAt: quest.CreatedAt,
            UpdatedAt: quest.UpdatedAt,
            SortOrder: quest.SortOrder,
            MyGameId: quest.MyGameId,
            GameName: quest.MyGame?.Game?.Name,
            SkillId: quest.SkillId,
            SkillName: quest.Skill?.Name,
            QuestFolderId: quest.QuestFolderId,
            QuestFolderName: quest.QuestFolder?.Name,
            Subtasks: quest.Subtasks
                .OrderBy(subtask => subtask.SortOrder)
                .ThenBy(subtask => subtask.Id)
                .Select(MapQuestSubtask)
                .ToList());
    }

    private static QuestSubtaskExport MapQuestSubtask(QuestSubtask subtask)
    {
        return new QuestSubtaskExport(
            Id: subtask.Id,
            Title: subtask.Title,
            Completed: subtask.Completed,
            CompletedAt: subtask.CompletedAt,
            CreatedAt: subtask.CreatedAt,
            SortOrder: subtask.SortOrder);
    }

    private static GamingSessionExport MapGamingSession(GamingSession session)
    {
        return new GamingSessionExport(
            Id: session.Id,
            MyGameId: session.MyGameId,
            GameName: session.MyGame?.Game?.Name,
            ScheduledAt: session.ScheduledAt,
            DurationMinutes: session.DurationMinutes,
            Completed: session.Completed,
            CompletedAt: session.CompletedAt,
            Notes: session.Notes,
            CreatedAt: session.CreatedAt);
    }

    private static UserGameAchievementExport MapUserGameAchievement(UserGameAchievement userAchievement)
    {
        var achievement = userAchievement.GameAchievement;
        return new UserGameAchievementExport(
            Id: userAchievement.Id,
            Provider: userAchievement.Provider.ToString(),
            SourceAchievementId: userAchievement.SourceAchievementId,
            UnlockedAt: userAchievement.UnlockedAt,
            SyncedAt: userAchievement.SyncedAt,
            Achievement: achievement is null
                ? null
                : new GameAchievementExport(
                    Id: achievement.Id,
                    GameId: achievement.GameId,
                    GameName: achievement.Game?.Name,
                    CanonicalKey: achievement.CanonicalKey,
                    Title: achievement.Title,
                    Description: achievement.Description,
                    IconUrl: achievement.IconUrl,
                    IsHidden: achievement.IsHidden,
                    LastSyncedAt: achievement.LastSyncedAt,
                    SteamApiName: achievement.SteamApiName,
                    SteamDisplayName: achievement.SteamDisplayName,
                    PsnTrophyId: achievement.PsnTrophyId,
                    PsnGroupId: achievement.PsnGroupId,
                    PsnTrophyType: achievement.PsnTrophyType,
                    PrimaryProvider: achievement.PrimaryProvider?.ToString()));
    }

    private static UserDocumentExport MapUserDocument(UserDocument document)
    {
        return new UserDocumentExport(
            Id: document.Id,
            Name: document.Name,
            StorageName: document.StorageName,
            Description: document.Description,
            Path: document.Path,
            ContentType: document.ContentType,
            DocumentType: document.DocumentType.ToString(),
            Url: document.Url,
            Album: document.Album);
    }

    private static FriendshipExport MapFriendship(Friendship friendship)
    {
        return new FriendshipExport(
            Id: friendship.Id,
            RequesterId: friendship.RequesterId,
            AddresseeId: friendship.AddresseeId,
            Status: friendship.Status.ToString(),
            CreatedAt: friendship.CreatedAt,
            RespondedAt: friendship.RespondedAt);
    }

    private static DirectMessageExport MapDirectMessage(DirectMessage message)
    {
        return new DirectMessageExport(
            Id: message.Id,
            SenderId: message.SenderId,
            RecipientId: message.RecipientId,
            Content: message.Content,
            SentAt: message.SentAt,
            ReadAt: message.ReadAt);
    }

    private static CalendarIntegrationExport MapCalendarIntegration(CalendarIntegration integration)
    {
        return new CalendarIntegrationExport(
            Id: integration.Id,
            Provider: integration.Provider,
            CalendarId: integration.CalendarId,
            AccountEmail: integration.AccountEmail,
            ConnectedAt: integration.ConnectedAt,
            LastSyncedAt: integration.LastSyncedAt);
    }
}
