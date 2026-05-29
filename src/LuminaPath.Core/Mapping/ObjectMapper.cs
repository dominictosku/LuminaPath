using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using System.Collections;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper : IObjectMapper
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

    private static object CreateInstance(Type type)
    {
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not create an instance of {type.FullName}.");
    }
}
