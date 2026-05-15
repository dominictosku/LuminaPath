using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Imports;

public sealed class GameImportPipeline
{
    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;

    public GameImportPipeline(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<GameImportPreviewResult> PreviewAsync(
        LuminaUser user,
        IEnumerable<GameImportItem> items,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await LoadGames(context)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new GameImportPreviewResult();
        var sequence = 1;

        foreach (var item in items)
        {
            sequence++;
            var rowNumber = item.RowNumber ?? sequence;
            var preview = new GameImportPreviewItem
            {
                RowNumber = rowNumber,
                Name = item.Name,
                Source = item.Source,
                ExternalId = item.ExternalId ?? string.Empty,
            };

            try
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    preview.Error = "Name is required.";
                    result.Errors.Add($"Row {rowNumber}: {preview.Error}");
                    result.Rows.Add(preview);
                    continue;
                }

                var game = FindGame(games, item);
                if (game is null)
                {
                    preview.GameAction = "Create";
                    preview.LibraryAction = "Create";
                    result.CreatedGames++;
                    result.CreatedMyGames++;
                }
                else
                {
                    preview.GameAction = "Update";
                    result.UpdatedGames++;

                    if (game.MyGames?.Any(myGame => myGame.LuminaUserId == user.Id) == true)
                    {
                        preview.LibraryAction = "Update";
                        result.UpdatedMyGames++;
                    }
                    else
                    {
                        preview.LibraryAction = "Create";
                        result.CreatedMyGames++;
                    }
                }

                result.RowsDetected++;
            }
            catch (Exception ex)
            {
                preview.Error = ex.Message;
                result.Errors.Add($"Row {rowNumber}: {ex.Message}");
            }

            result.Rows.Add(preview);
        }

        return result;
    }

    public async Task<GameImportResult> ImportAsync(
        LuminaUser user,
        IEnumerable<GameImportItem> items,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var games = await LoadGames(context).ToListAsync(cancellationToken);
        var names = new HashSet<string>(games.Select(game => game.Name), StringComparer.OrdinalIgnoreCase);
        var result = new GameImportResult();
        var sequence = 1;

        foreach (var item in items)
        {
            sequence++;
            var rowNumber = item.RowNumber ?? sequence;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            try
            {
                var game = FindGame(games, item);
                if (game is null)
                {
                    game = CreateGame(item, names);
                    games.Add(game);
                    context.Games.Add(game);
                    result.CreatedGames++;
                }
                else
                {
                    ApplyGameValues(game, item);
                    result.UpdatedGames++;
                }

                EnsureExternalId(game, item);
                game.MyGames ??= new List<MyGame>();
                var myGame = game.MyGames.FirstOrDefault(entry => entry.LuminaUserId == user.Id);
                if (myGame is null)
                {
                    myGame = new MyGame
                    {
                        Game = game,
                        LuminaUserId = user.Id
                    };
                    game.MyGames.Add(myGame);
                    context.MyGames.Add(myGame);
                    result.CreatedMyGames++;
                }
                else
                {
                    result.UpdatedMyGames++;
                }

                ApplyMyGameValues(myGame, item);
                result.RowsImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            result.Errors.Add($"Database save failed: {ex.InnerException?.Message ?? ex.Message}");
        }

        return result;
    }

    private static IQueryable<Game> LoadGames(LuminaPathDbContext context)
    {
        return context.Games
            .Include(game => game.ExternalIds)
            .Include(game => game.MyGames!)
                .ThenInclude(myGame => myGame.MyGameInfo);
    }

    private static Game? FindGame(IEnumerable<Game> games, GameImportItem item)
    {
        var normalizedExternalId = NormalizeExternalId(item.ExternalId);
        if (item.ExternalProvider is not null && !string.IsNullOrWhiteSpace(normalizedExternalId))
        {
            var byExternalId = games.FirstOrDefault(game =>
                game.ExternalIds.Any(externalId =>
                    externalId.Provider == item.ExternalProvider
                    && externalId.ExternalId.Equals(normalizedExternalId, StringComparison.OrdinalIgnoreCase)));

            if (byExternalId is not null)
            {
                return byExternalId;
            }

        }

        return games.FirstOrDefault(game => game.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static Game CreateGame(GameImportItem item, HashSet<string> names)
    {
        var game = new Game
        {
            Name = ResolveUniqueName(item.Name.Trim(), names, item.ExternalProvider, item.ExternalId),
            Source = item.Source,
        };

        names.Add(game.Name);
        ApplyGameValues(game, item);
        return game;
    }

    private static void ApplyGameValues(Game game, GameImportItem item)
    {
        game.Description = item.Description ?? game.Description;
        game.Source = string.IsNullOrWhiteSpace(item.Source) ? game.Source : item.Source;
        game.ReleaseDate = item.ReleaseDate ?? game.ReleaseDate;
        game.Playtime = item.Playtime ?? game.Playtime;

        if (item.Platforms != 0)
        {
            game.Platforms = game.Platforms == 0
                ? item.Platforms
                : game.Platforms | item.Platforms;
        }

        if (item.Genres.Count > 0)
        {
            game.Genres = item.Genres;
        }
    }

    private static void ApplyMyGameValues(MyGame myGame, GameImportItem item)
    {
        myGame.Status = item.Status;
        myGame.Priority = item.Priority;
        myGame.Rating = item.Rating ?? myGame.Rating;
        myGame.StartDate = item.StartDate ?? myGame.StartDate;
        myGame.EndDate = item.EndDate ?? myGame.EndDate;
        myGame.TimeSpend = item.TimeSpend ?? myGame.TimeSpend;

        if (item.FirstPlayed.HasValue || item.LastPlayed.HasValue || item.TrackedHours.HasValue)
        {
            myGame.MyGameInfo ??= new MyGameInfo
            {
                FirstPlayed = UtcDateTime.Normalize(DateTime.MinValue),
                LastPlayed = UtcDateTime.Normalize(DateTime.MinValue)
            };

            myGame.MyGameInfo.FirstPlayed = item.FirstPlayed ?? myGame.MyGameInfo.FirstPlayed;
            myGame.MyGameInfo.LastPlayed = item.LastPlayed ?? myGame.MyGameInfo.LastPlayed;
            myGame.MyGameInfo.TrackedHours = item.TrackedHours ?? myGame.MyGameInfo.TrackedHours;
        }
    }

    private static void EnsureExternalId(Game game, GameImportItem item)
    {
        var normalizedExternalId = NormalizeExternalId(item.ExternalId);
        if (item.ExternalProvider is null || string.IsNullOrWhiteSpace(normalizedExternalId))
        {
            return;
        }

        game.ExternalIds ??= new List<MediaExternalId>();
        if (game.ExternalIds.Any(externalId =>
                externalId.Provider == item.ExternalProvider
                && externalId.ExternalId.Equals(normalizedExternalId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        game.ExternalIds.Add(new MediaExternalId
        {
            Provider = item.ExternalProvider.Value,
            ExternalId = normalizedExternalId
        });
    }

    private static string? NormalizeExternalId(string? externalId)
        => string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();

    private static string ResolveUniqueName(
        string desired,
        HashSet<string> names,
        ExternalMediaProvider? provider,
        string? externalId)
    {
        if (!names.Contains(desired))
        {
            return desired;
        }

        var suffix = provider is null || string.IsNullOrWhiteSpace(externalId)
            ? "Import"
            : $"{provider} {externalId}";

        return $"{desired} ({suffix})";
    }
}
