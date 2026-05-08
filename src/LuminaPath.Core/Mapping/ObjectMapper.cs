using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.Third_Party;
using System.Collections;
using System.Reflection;

namespace LuminaPath.Core.Mapping
{
    public class ObjectMapper : IObjectMapper
    {
        public TDestination Map<TDestination>(object? source)
        {
            return (TDestination)MapObject(source, typeof(TDestination));
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return Map<TDestination>(source);
        }

        private object MapObject(object? source, Type destinationType)
        {
            if (source == null)
            {
                return destinationType.IsValueType ? Activator.CreateInstance(destinationType)! : null!;
            }

            if (TryMapCollection(source, destinationType, out var collection))
            {
                return collection;
            }

            return source switch
            {
                MyGameDto dto when destinationType == typeof(MyGame) => MapMyGame(dto),
                MyGame entity when destinationType == typeof(MyGame) => MapMyGame(entity),
                MyGame entity when destinationType == typeof(MyGameDto) => MapMyGameDto(entity),
                GamesNoIncludeDto dto when destinationType == typeof(Game) => MapGame(dto),
                GamesDto dto when destinationType == typeof(Game) => MapGame(dto),
                Game entity when destinationType == typeof(GamesNoIncludeDto) => MapGamesNoIncludeDto(entity),
                Game entity when destinationType == typeof(GamesDto) => MapGamesDto(entity),
                Game entity when typeof(Game).IsAssignableFrom(destinationType) => MapGameToDestination(entity, destinationType),
                _ when destinationType.IsAssignableFrom(source.GetType()) => source,
                _ => CopyMatchingProperties(source, CreateInstance(destinationType))
            };
        }

        private bool TryMapCollection(object source, Type destinationType, out object collection)
        {
            collection = null!;

            if (source is string || source is not IEnumerable enumerable)
            {
                return false;
            }

            var itemType = GetEnumerableItemType(destinationType);
            if (itemType == null)
            {
                return false;
            }

            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType)!;

            foreach (var item in enumerable)
            {
                list.Add(MapObject(item, itemType));
            }

            collection = list;
            return true;
        }

        private static Type? GetEnumerableItemType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
        }

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
                GameInfo = source.GameInfo
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
                GameInfo = source.GameInfo
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
                LuminaUser = source.LuminaUser,
                GameId = source.GameId,
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
                Rating = source.Rating.HasValue ? Convert.ToByte(source.Rating.Value) : null,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend.HasValue ? Convert.ToInt32(source.TimeSpend.Value) : null,
                GameId = source.GameId,
                Game = source.Game == null ? null : Map<GamesNoIncludeDto>(source.Game),
                MyGameInfo = MapMyGameInfo(source.MyGameInfo) ?? new()
            };
        }

        private static TDocument? MapDocument<TDocument>(Document? source) where TDocument : Document, new()
        {
            if (source == null)
            {
                return null;
            }

            if (source is TDocument document)
            {
                return document;
            }

            return new TDocument
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Path = source.Path,
                ContentType = source.ContentType,
                DocumentType = source.DocumentType
            };
        }

        private static void CopyGameProperties(Game source, object destination)
        {
            CopyMatchingProperties(source, destination);
        }

        private void NormalizeGameRelationships(Game source, Game destination)
        {
            if (source.GameInfo == null)
            {
                destination.GameInfo = null;
            }
            else
            {
                destination.GameInfo = MapGameInfo(source.GameInfo);
                destination.GameInfo.Game = destination;
                destination.GameInfo.GameId = destination.Id > 0 ? destination.Id : 0;
            }

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

        private static GameInfo MapGameInfo(GameInfo source)
        {
            return new GameInfo
            {
                Id = source.Id,
                GameId = source.GameId,
                PsnId = source.PsnId,
                SteamId = source.SteamId
            };
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

        private static object CopyMatchingProperties(object source, object destination)
        {
            var sourceProperties = source.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name);

            foreach (var destinationProperty in destination.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite))
            {
                if (!sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
                {
                    continue;
                }

                if (!destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                {
                    continue;
                }

                destinationProperty.SetValue(destination, sourceProperty.GetValue(source));
            }

            return destination;
        }

        private static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Could not create an instance of {type.FullName}.");
        }

        private static List<string> SplitGenres(string? genre)
        {
            return string.IsNullOrWhiteSpace(genre)
                ? []
                : genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static string JoinGenres(List<string>? genres)
        {
            return genres == null ? string.Empty : string.Join(", ", genres);
        }
    }
}
