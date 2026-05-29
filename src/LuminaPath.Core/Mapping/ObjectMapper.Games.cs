using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.ThirdParty;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper
{
    private object MapGameToDestination(Game source, Type destinationType)
    {
        var destination = CreateInstance(destinationType);
        CopyGameProperties(source, destination);

        if (destination is Game game)
        {
            NormalizeGameRelationships(source, game);
        }

        return destination;
    }

    private static Game MapGame(GamesNoIncludeDto source)
    {
        return new Game
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genres = SplitGenres(source.Genre),
            ReleaseDate = source.ReleaseDate,
            Platforms = source.Platforms,
            Playtime = source.Playtime,
            Source = source.Source,
            Image = MapDocument<MediaDocument>(source.Image),
            ParentGameId = source.ParentGameId,
            ExternalIds = MapGameExternalIds(source.PsnId, source.SteamId)
        };
    }

    private Game MapGame(GamesDto source)
    {
        return new Game
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genres = SplitGenres(source.Genre),
            ReleaseDate = source.ReleaseDate,
            Platforms = source.Platforms,
            Playtime = source.Playtime,
            Image = MapDocument<MediaDocument>(source.Image),
            ParentGameId = source.ParentGameId,
            MyGames = source.MyGames == null ? null : [Map<MyGame>(source.MyGames)]
        };
    }

    private GamesNoIncludeDto MapGamesNoIncludeDto(Game source)
    {
        return new GamesNoIncludeDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genre = JoinGenres(source.Genres),
            ReleaseDate = source.ReleaseDate,
            Platforms = source.Platforms,
            Playtime = source.Playtime,
            Source = source.Source,
            Image = MapDocument<Document>(source.Image),
            ParentGameId = source.ParentGameId,
            PsnId = source.ExternalIds.GetExternalId(ExternalMediaProvider.Psn),
            SteamId = source.ExternalIds.GetExternalId(ExternalMediaProvider.Steam)
        };
    }

    private GamesDto MapGamesDto(Game source)
    {
        return new GamesDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genre = JoinGenres(source.Genres),
            ReleaseDate = source.ReleaseDate,
            Platforms = source.Platforms,
            Playtime = source.Playtime,
            Image = MapDocument<Document>(source.Image),
            ParentGameId = source.ParentGameId,
            ParentGameName = source.ParentGame?.Name,
            Dlcs = source.Dlcs?.Select(MapGamesNoIncludeDto).ToList(),
            MyGames = source.MyGames == null ? null : Map<MyGameDto>(source.MyGames.FirstOrDefault())
        };
    }

    private MyGame MapMyGame(MyGameDto source)
    {
        return new MyGame
        {
            Id = source.Id,
            Rating = source.Rating,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = source.TimeSpend,
            GameId = source.GameId,
            PersonalNotes = source.PersonalNotes,
            Game = source.Game == null ? null : Map<Game>(source.Game),
            MyGameInfo = MapMyGameInfo(source.MyGameInfo)
        };
    }

    private static MyGame MapMyGame(MyGame source)
    {
        var destination = new MyGame
        {
            Id = source.Id,
            Rating = source.Rating,
            Priority = source.Priority,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = source.TimeSpend,
            LuminaUserId = source.LuminaUserId,
            GameId = source.GameId,
            PersonalNotes = source.PersonalNotes,
            MyGameInfo = MapMyGameInfo(source.MyGameInfo)
        };

        if (destination.MyGameInfo != null)
        {
            destination.MyGameInfo.Game = destination;
            destination.MyGameInfo.MyGameId = destination.Id > 0 ? destination.Id : 0;
        }

        return destination;
    }

    private MyGameDto MapMyGameDto(MyGame source)
    {
        return new MyGameDto
        {
            Id = source.Id,
            Rating = ToDtoRating(source.Rating),
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = ToDtoTimeSpend(source.TimeSpend),
            GameId = source.GameId,
            PersonalNotes = source.PersonalNotes,
            Game = source.Game == null ? null : Map<GamesNoIncludeDto>(source.Game),
            MyGameInfo = MapMyGameInfo(source.MyGameInfo) ?? new()
        };
    }

    private static void CopyGameProperties(Game source, object destination)
    {
        CopyMatchingProperties(source, destination);
    }

    private void NormalizeGameRelationships(Game source, Game destination)
    {
        destination.ExternalIds = MapExternalIds(source.ExternalIds, destination.Id);

        if (source.MyGames == null)
        {
            destination.MyGames = null;
            return;
        }

        destination.MyGames = source.MyGames
            .Select(myGame =>
            {
                var mappedMyGame = Map<MyGame>(myGame);
                mappedMyGame.Game = destination;
                mappedMyGame.GameId = destination.Id > 0 ? destination.Id : 0;
                return mappedMyGame;
            })
            .ToList();
    }

    private static List<MediaExternalId> MapGameExternalIds(string? psnId, string? steamId)
    {
        var externalIds = new List<MediaExternalId>();
        externalIds.SetExternalId(ExternalMediaProvider.Psn, psnId);
        externalIds.SetExternalId(ExternalMediaProvider.Steam, steamId);
        return externalIds;
    }

    private static List<MediaExternalId> MapExternalIds(IEnumerable<MediaExternalId>? source, int mediaId)
    {
        return source?
            .Where(externalId => !string.IsNullOrWhiteSpace(externalId.ExternalId))
            .GroupBy(externalId => externalId.Provider)
            .Select(group =>
            {
                var externalId = group.First();
                return new MediaExternalId
                {
                    Id = externalId.Id,
                    MediaId = mediaId > 0 ? mediaId : externalId.MediaId,
                    Provider = externalId.Provider,
                    ExternalId = externalId.ExternalId.Trim()
                };
            })
            .ToList() ?? new List<MediaExternalId>();
    }

    private static MyGameInfo? MapMyGameInfo(MyGameInfo? source)
    {
        if (source == null)
        {
            return null;
        }

        return new MyGameInfo
        {
            Id = source.Id,
            MyGameId = source.MyGameId,
            TrackedHours = source.TrackedHours,
            FirstPlayed = source.FirstPlayed,
            LastPlayed = source.LastPlayed
        };
    }
}
