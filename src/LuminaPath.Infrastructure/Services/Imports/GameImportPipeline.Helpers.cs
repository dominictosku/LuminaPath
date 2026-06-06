using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.Imports;

public sealed partial class GameImportPipeline
{
    private static readonly ExternalMediaProvider[] ExternalIdImportOrder =
    [
        ExternalMediaProvider.Psn,
        ExternalMediaProvider.Steam,
        ExternalMediaProvider.Igdb,
        ExternalMediaProvider.Rawg,
        ExternalMediaProvider.Excel
    ];

    private static IQueryable<Game> LoadGames(LuminaPathDbContext context)
    {
        return context.Games
            .Include(game => game.ExternalIds)
            .Include(game => game.MyGames!)
                .ThenInclude(myGame => myGame.MyGameInfo);
    }

    private static Game? FindGame(IEnumerable<Game> games, GameImportItem item)
    {
        foreach (var candidateExternalId in GetExternalIds(item))
        {
            var byExternalId = games.FirstOrDefault(game =>
                game.ExternalIds.Any(externalId =>
                    externalId.Provider == candidateExternalId.Key
                    && externalId.ExternalId.Equals(candidateExternalId.Value, StringComparison.OrdinalIgnoreCase)));

            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        return games.FirstOrDefault(game => game.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static Game CreateGame(GameImportItem item, HashSet<string> names)
    {
        var primaryExternalId = GetExternalIds(item).FirstOrDefault();
        var game = new Game
        {
            Name = ResolveUniqueName(
                item.Name.Trim(),
                names,
                primaryExternalId.Key == default ? null : primaryExternalId.Key,
                primaryExternalId.Value),
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
        game.ExternalIds ??= new List<MediaExternalId>();
        foreach (var candidateExternalId in GetExternalIds(item))
        {
            if (game.ExternalIds.Any(externalId =>
                    externalId.Provider == candidateExternalId.Key
                    && externalId.ExternalId.Equals(candidateExternalId.Value, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            game.ExternalIds.Add(new MediaExternalId
            {
                Provider = candidateExternalId.Key,
                ExternalId = candidateExternalId.Value
            });
        }
    }

    private static string? NormalizeExternalId(string? externalId)
        => string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();

    private static IEnumerable<KeyValuePair<ExternalMediaProvider, string>> GetExternalIds(GameImportItem item)
    {
        var returnedProviders = new HashSet<ExternalMediaProvider>();

        foreach (var provider in ExternalIdImportOrder)
        {
            if (item.ExternalIds.TryGetValue(provider, out var externalId))
            {
                var normalizedExternalId = NormalizeExternalId(externalId);
                if (!string.IsNullOrWhiteSpace(normalizedExternalId))
                {
                    returnedProviders.Add(provider);
                    yield return new KeyValuePair<ExternalMediaProvider, string>(provider, normalizedExternalId);
                }
            }
        }

        foreach (var externalId in item.ExternalIds)
        {
            if (returnedProviders.Contains(externalId.Key))
            {
                continue;
            }

            var normalizedExternalId = NormalizeExternalId(externalId.Value);
            if (!string.IsNullOrWhiteSpace(normalizedExternalId))
            {
                returnedProviders.Add(externalId.Key);
                yield return new KeyValuePair<ExternalMediaProvider, string>(externalId.Key, normalizedExternalId);
            }
        }

        var fallbackExternalId = NormalizeExternalId(item.ExternalId);
        if (item.ExternalProvider is not null
            && !returnedProviders.Contains(item.ExternalProvider.Value)
            && !string.IsNullOrWhiteSpace(fallbackExternalId))
        {
            yield return new KeyValuePair<ExternalMediaProvider, string>(item.ExternalProvider.Value, fallbackExternalId);
        }
    }

    private static string GetImportKey(GameImportItem item)
    {
        var externalId = GetExternalIds(item).FirstOrDefault();
        if (externalId.Key != default && !string.IsNullOrWhiteSpace(externalId.Value))
        {
            return $"{externalId.Key}:{externalId.Value}";
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
