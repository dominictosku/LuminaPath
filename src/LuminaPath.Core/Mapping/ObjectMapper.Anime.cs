using LuminaPath.Core.Dtos;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper
{
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
            Rating = ToDtoRating(source.Rating),
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = ToDtoTimeSpend(source.TimeSpend),
            AnimeId = source.AnimeId,
            Anime = source.Anime == null ? null : Map<AnimesNoIncludeDto>(source.Anime),
            CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
            CurrentEpisode = source.CurrentEpisode
        };
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
}
