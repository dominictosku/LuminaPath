using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Imports;

public sealed partial class GameImportPipeline
{
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

    private static string GetImportKey(GameImportItem item)
    {
        var externalId = NormalizeExternalId(item.ExternalId);
        if (item.ExternalProvider is not null && !string.IsNullOrWhiteSpace(externalId))
        {
            return $"{item.ExternalProvider}:{externalId}";
        }

        return $"name:{item.Name.Trim()}";
    }

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
