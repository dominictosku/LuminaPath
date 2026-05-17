using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
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
                MyAnimeDto dto when destinationType == typeof(MyAnime) => MapMyAnime(dto),
                MyAnime entity when destinationType == typeof(MyAnimeDto) => MapMyAnimeDto(entity),
                MyMovieDto dto when destinationType == typeof(MyMovie) => MapMyMovie(dto),
                MyMovie entity when destinationType == typeof(MyMovieDto) => MapMyMovieDto(entity),
                GamesNoIncludeDto dto when destinationType == typeof(Game) => MapGame(dto),
                GamesDto dto when destinationType == typeof(Game) => MapGame(dto),
                Game entity when destinationType == typeof(GamesNoIncludeDto) => MapGamesNoIncludeDto(entity),
                Game entity when destinationType == typeof(GamesDto) => MapGamesDto(entity),
                Game entity when typeof(Game).IsAssignableFrom(destinationType) => MapGameToDestination(entity, destinationType),
                AnimesNoIncludeDto dto when destinationType == typeof(Anime) => MapAnime(dto),
                AnimesDto dto when destinationType == typeof(Anime) => MapAnime(dto),
                Anime entity when destinationType == typeof(AnimesNoIncludeDto) => MapAnimesNoIncludeDto(entity),
                Anime entity when destinationType == typeof(AnimesDto) => MapAnimesDto(entity),
                Anime entity when typeof(Anime).IsAssignableFrom(destinationType) => MapAnimeToDestination(entity, destinationType),
                MyAnime entity when destinationType == typeof(MyAnime) => MapMyAnime(entity),
                MoviesNoIncludeDto dto when destinationType == typeof(Movie) => MapMovie(dto),
                MoviesDto dto when destinationType == typeof(Movie) => MapMovie(dto),
                Movie entity when destinationType == typeof(MoviesNoIncludeDto) => MapMoviesNoIncludeDto(entity),
                Movie entity when destinationType == typeof(MoviesDto) => MapMoviesDto(entity),
                Movie entity when typeof(Movie).IsAssignableFrom(destinationType) => MapMovieToDestination(entity, destinationType),
                MyMovie entity when destinationType == typeof(MyMovie) => MapMyMovie(entity),
                SeriesNoIncludeDto dto when destinationType == typeof(Series) => MapSeries(dto),
                SeriesDto dto when destinationType == typeof(Series) => MapSeries(dto),
                Series entity when destinationType == typeof(SeriesNoIncludeDto) => MapSeriesNoIncludeDto(entity),
                Series entity when destinationType == typeof(SeriesDto) => MapSeriesDto(entity),
                Series entity when typeof(Series).IsAssignableFrom(destinationType) => MapSeriesToDestination(entity, destinationType),
                MySeriesDto dto when destinationType == typeof(MySeries) => MapMySeries(dto),
                MySeries entity when destinationType == typeof(MySeriesDto) => MapMySeriesDto(entity),
                MySeries entity when destinationType == typeof(MySeries) => MapMySeries(entity),
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
                Rating = source.Rating.HasValue ? Convert.ToByte(source.Rating.Value) : null,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend.HasValue ? Convert.ToInt32(source.TimeSpend.Value) : null,
                GameId = source.GameId,
                PersonalNotes = source.PersonalNotes,
                Game = source.Game == null ? null : Map<GamesNoIncludeDto>(source.Game),
                MyGameInfo = MapMyGameInfo(source.MyGameInfo) ?? new()
            };
        }

        private static Anime MapAnime(AnimesNoIncludeDto source)
        {
            return new Anime
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = ResolveAnimePerEpisodeMinutes(source.ExpectedWatchTimePerEpisodeMinutes, source.ExpectedWatchTimeMinutes, source.EpisodeCount),
                EpisodeCount = source.EpisodeCount,
                Source = source.Source,
                Image = MapDocument<MediaDocument>(source.Image),
                ParentAnimeId = source.ParentAnimeId
            };
        }

        private Anime MapAnime(AnimesDto source)
        {
            return new Anime
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = ResolveAnimePerEpisodeMinutes(source.ExpectedWatchTimePerEpisodeMinutes, source.ExpectedWatchTimeMinutes, source.EpisodeCount),
                EpisodeCount = source.EpisodeCount,
                Image = MapDocument<MediaDocument>(source.Image),
                ParentAnimeId = source.ParentAnimeId,
                MyAnimes = source.MyAnimes == null ? null : [Map<MyAnime>(source.MyAnimes)]
            };
        }

        private static AnimesNoIncludeDto MapAnimesNoIncludeDto(Anime source)
        {
            return new AnimesNoIncludeDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = source.ExpectedWatchTimePerEpisodeMinutes,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                EpisodeCount = source.EpisodeCount,
                Source = source.Source,
                Image = MapDocument<Document>(source.Image),
                ParentAnimeId = source.ParentAnimeId
            };
        }

        private AnimesDto MapAnimesDto(Anime source)
        {
            return new AnimesDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = source.ExpectedWatchTimePerEpisodeMinutes,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                EpisodeCount = source.EpisodeCount,
                Image = MapDocument<Document>(source.Image),
                ParentAnimeId = source.ParentAnimeId,
                ParentAnimeName = source.ParentAnime?.Name,
                Seasons = source.Seasons?.Select(MapAnimesNoIncludeDto).ToList(),
                MyAnimes = source.MyAnimes == null ? null : Map<MyAnimeDto>(source.MyAnimes.FirstOrDefault())
            };
        }

        private MyAnime MapMyAnime(MyAnimeDto source)
        {
            return new MyAnime
            {
                Id = source.Id,
                Rating = source.Rating,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                AnimeId = source.AnimeId,
                Anime = source.Anime == null ? null : Map<Anime>(source.Anime),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
            }.WithCalculatedAnimeWatchTime();
        }

        private MyAnime MapMyAnime(MyAnime source)
        {
            return new MyAnime
            {
                Id = source.Id,
                Rating = source.Rating,
                Priority = source.Priority,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                LuminaUserId = source.LuminaUserId,
                AnimeId = source.AnimeId,
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
            }.WithCalculatedAnimeWatchTime(source.Anime);
        }

        private MyAnimeDto MapMyAnimeDto(MyAnime source)
        {
            return new MyAnimeDto
            {
                Id = source.Id,
                Rating = source.Rating.HasValue ? Convert.ToByte(source.Rating.Value) : null,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend.HasValue ? Convert.ToInt32(source.TimeSpend.Value) : null,
                AnimeId = source.AnimeId,
                Anime = source.Anime == null ? null : Map<AnimesNoIncludeDto>(source.Anime),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
            };
        }

        private static Movie MapMovie(MoviesNoIncludeDto source)
        {
            return new Movie
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                Source = source.Source,
                Image = MapDocument<MediaDocument>(source.Image)
            };
        }

        private Movie MapMovie(MoviesDto source)
        {
            return new Movie
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                Image = MapDocument<MediaDocument>(source.Image),
                MyMovies = source.MyMovies == null ? null : [Map<MyMovie>(source.MyMovies)]
            };
        }

        private MoviesNoIncludeDto MapMoviesNoIncludeDto(Movie source)
        {
            return new MoviesNoIncludeDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                Source = source.Source,
                Image = MapDocument<Document>(source.Image)
            };
        }

        private MoviesDto MapMoviesDto(Movie source)
        {
            return new MoviesDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                Image = MapDocument<Document>(source.Image),
                MyMovies = source.MyMovies == null ? null : Map<MyMovieDto>(source.MyMovies.FirstOrDefault())
            };
        }

        private MyMovie MapMyMovie(MyMovieDto source)
        {
            return new MyMovie
            {
                Id = source.Id,
                Rating = source.Rating,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                MovieId = source.MovieId,
                Movie = source.Movie == null ? null : Map<Movie>(source.Movie),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
            };
        }

        private MyMovie MapMyMovie(MyMovie source)
        {
            return new MyMovie
            {
                Id = source.Id,
                Rating = source.Rating,
                Priority = source.Priority,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                LuminaUserId = source.LuminaUserId,
                MovieId = source.MovieId,
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
            };
        }

        private MyMovieDto MapMyMovieDto(MyMovie source)
        {
            return new MyMovieDto
            {
                Id = source.Id,
                Rating = source.Rating.HasValue ? Convert.ToByte(source.Rating.Value) : null,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend.HasValue ? Convert.ToInt32(source.TimeSpend.Value) : null,
                MovieId = source.MovieId,
                Movie = source.Movie == null ? null : Map<MoviesNoIncludeDto>(source.Movie),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
            };
        }

        private static Series MapSeries(SeriesNoIncludeDto source)
        {
            return new Series
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = ResolveAnimePerEpisodeMinutes(source.ExpectedWatchTimePerEpisodeMinutes, source.ExpectedWatchTimeMinutes, source.EpisodeCount),
                EpisodeCount = source.EpisodeCount,
                Source = source.Source,
                Image = MapDocument<MediaDocument>(source.Image),
                ParentSeriesId = source.ParentSeriesId
            };
        }

        private Series MapSeries(SeriesDto source)
        {
            return new Series
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genres = SplitGenres(source.Genre),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = ResolveAnimePerEpisodeMinutes(source.ExpectedWatchTimePerEpisodeMinutes, source.ExpectedWatchTimeMinutes, source.EpisodeCount),
                EpisodeCount = source.EpisodeCount,
                Image = MapDocument<MediaDocument>(source.Image),
                ParentSeriesId = source.ParentSeriesId,
                MySeries = source.MySeries == null ? null : [Map<MySeries>(source.MySeries)]
            };
        }

        private static SeriesNoIncludeDto MapSeriesNoIncludeDto(Series source)
        {
            return new SeriesNoIncludeDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = source.ExpectedWatchTimePerEpisodeMinutes,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                EpisodeCount = source.EpisodeCount,
                Source = source.Source,
                Image = MapDocument<Document>(source.Image),
                ParentSeriesId = source.ParentSeriesId
            };
        }

        private SeriesDto MapSeriesDto(Series source)
        {
            return new SeriesDto
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Genre = JoinGenres(source.Genres),
                ReleaseDate = source.ReleaseDate,
                ExpectedWatchTimePerEpisodeMinutes = source.ExpectedWatchTimePerEpisodeMinutes,
                ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
                EpisodeCount = source.EpisodeCount,
                Image = MapDocument<Document>(source.Image),
                ParentSeriesId = source.ParentSeriesId,
                ParentSeriesName = source.ParentSeries?.Name,
                Seasons = source.Seasons?.Select(MapSeriesNoIncludeDto).ToList(),
                MySeries = source.MySeries == null ? null : Map<MySeriesDto>(source.MySeries.FirstOrDefault())
            };
        }

        private MySeries MapMySeries(MySeriesDto source)
        {
            return new MySeries
            {
                Id = source.Id,
                Rating = source.Rating,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                SeriesId = source.SeriesId,
                Series = source.Series == null ? null : Map<Series>(source.Series),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
            }.WithCalculatedSeriesWatchTime();
        }

        private MySeries MapMySeries(MySeries source)
        {
            return new MySeries
            {
                Id = source.Id,
                Rating = source.Rating,
                Priority = source.Priority,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend,
                LuminaUserId = source.LuminaUserId,
                SeriesId = source.SeriesId,
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
            }.WithCalculatedSeriesWatchTime(source.Series);
        }

        private MySeriesDto MapMySeriesDto(MySeries source)
        {
            return new MySeriesDto
            {
                Id = source.Id,
                Rating = source.Rating.HasValue ? Convert.ToByte(source.Rating.Value) : null,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Status = source.Status,
                TimeSpend = source.TimeSpend.HasValue ? Convert.ToInt32(source.TimeSpend.Value) : null,
                SeriesId = source.SeriesId,
                Series = source.Series == null ? null : Map<SeriesNoIncludeDto>(source.Series),
                CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
                CurrentEpisode = source.CurrentEpisode
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
                StorageName = source.StorageName,
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

        private object MapAnimeToDestination(Anime source, Type destinationType)
        {
            var destination = CreateInstance(destinationType);
            CopyMatchingProperties(source, destination);

            if (destination is Anime anime)
            {
                NormalizeAnimeRelationships(source, anime);
            }

            return destination;
        }

        private void NormalizeAnimeRelationships(Anime source, Anime destination)
        {
            if (source.MyAnimes == null)
            {
                destination.MyAnimes = null;
                return;
            }

            destination.MyAnimes = source.MyAnimes
                .Select(myAnime =>
                {
                    var mappedMyAnime = Map<MyAnime>(myAnime);
                    mappedMyAnime.Anime = destination;
                    mappedMyAnime.AnimeId = destination.Id > 0 ? destination.Id : 0;
                    return mappedMyAnime;
                })
                .ToList();
        }

        private object MapMovieToDestination(Movie source, Type destinationType)
        {
            var destination = CreateInstance(destinationType);
            CopyMatchingProperties(source, destination);

            if (destination is Movie movie)
            {
                NormalizeMovieRelationships(source, movie);
            }

            return destination;
        }

        private void NormalizeMovieRelationships(Movie source, Movie destination)
        {
            if (source.MyMovies == null)
            {
                destination.MyMovies = null;
                return;
            }

            destination.MyMovies = source.MyMovies
                .Select(myMovie =>
                {
                    var mappedMyMovie = Map<MyMovie>(myMovie);
                    mappedMyMovie.Movie = destination;
                    mappedMyMovie.MovieId = destination.Id > 0 ? destination.Id : 0;
                    return mappedMyMovie;
                })
                .ToList();
        }

        private object MapSeriesToDestination(Series source, Type destinationType)
        {
            var destination = CreateInstance(destinationType);
            CopyMatchingProperties(source, destination);

            if (destination is Series series)
            {
                NormalizeSeriesRelationships(source, series);
            }

            return destination;
        }

        private void NormalizeSeriesRelationships(Series source, Series destination)
        {
            if (source.MySeries == null)
            {
                destination.MySeries = null;
                return;
            }

            destination.MySeries = source.MySeries
                .Select(mySeries =>
                {
                    var mappedMySeries = Map<MySeries>(mySeries);
                    mappedMySeries.Series = destination;
                    mappedMySeries.SeriesId = destination.Id > 0 ? destination.Id : 0;
                    mappedMySeries.RecalculateWatchTime(destination);
                    return mappedMySeries;
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

        private static int? ResolveAnimePerEpisodeMinutes(int? perEpisodeMinutes, int? totalMinutes, int? episodeCount)
        {
            if (perEpisodeMinutes is not null)
            {
                return perEpisodeMinutes;
            }

            if (totalMinutes is null || episodeCount is not > 0)
            {
                return totalMinutes;
            }

            return Math.Max(1, (int)Math.Round(totalMinutes.Value / (double)episodeCount.Value));
        }
    }

    internal static class AnimeWatchTimeMappingExtensions
    {
        public static MyAnime WithCalculatedAnimeWatchTime(this MyAnime myAnime, Anime? anime = null)
        {
            myAnime.RecalculateWatchTime(anime);
            return myAnime;
        }

        public static MySeries WithCalculatedSeriesWatchTime(this MySeries mySeries, Series? series = null)
        {
            mySeries.RecalculateWatchTime(series);
            return mySeries;
        }
    }
}
