using LuminaPath.Core.Models;

namespace LuminaPath.Core.Mapping
{
    internal static class WatchTimeMappingExtensions
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
