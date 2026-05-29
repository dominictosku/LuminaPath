using LuminaPath.Core.Dtos;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper
{
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
            Rating = ToDtoRating(source.Rating),
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = ToDtoTimeSpend(source.TimeSpend),
            SeriesId = source.SeriesId,
            Series = source.Series == null ? null : Map<SeriesNoIncludeDto>(source.Series),
            CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes,
            CurrentEpisode = source.CurrentEpisode
        };
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
}
