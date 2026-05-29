using LuminaPath.Core.Models.Base;
using System.Reflection;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper
{
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

    private static byte? ToDtoRating(short? rating)
    {
        return rating.HasValue ? Convert.ToByte(rating.Value) : null;
    }

    private static double? ToDtoTimeSpend(double? timeSpend)
    {
        return timeSpend;
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
