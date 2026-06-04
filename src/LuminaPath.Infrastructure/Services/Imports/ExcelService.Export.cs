using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    private static readonly string[] GameHeaders =
    [
        "Id",
        "Name",
        "Status",
        "Priority",
        "Release Date",
        "Platform",
        "Genre",
        "Source",
        "Description",
        "Playtime",
        "PSNId",
        "SteamId",
        "IGDBId",
        "RAWGId",
        "Parent Game Id",
        "Image Url",
        "Rating",
        "Start Date",
        "End Date",
        "Time Spend",
        "First Played",
        "Last Played",
        "Tracked Hours",
        "Personal Notes"
    ];

    private static readonly string[] AnimeHeaders =
    [
        "Id",
        "Name",
        "Status",
        "Priority",
        "Release Date",
        "Genre",
        "Source",
        "Description",
        "AniListId",
        "MALId",
        "Episode Count",
        "Expected Minutes Per Episode",
        "Expected Watch Time Minutes",
        "Parent Anime Id",
        "Image Url",
        "Rating",
        "Start Date",
        "End Date",
        "Time Spend",
        "Current Episode",
        "Current Watch Time Minutes"
    ];

    private static readonly string[] MovieHeaders =
    [
        "Id",
        "Name",
        "Status",
        "Priority",
        "Release Date",
        "Genre",
        "Source",
        "Description",
        "TMDBId",
        "Expected Watch Time Minutes",
        "Image Url",
        "Rating",
        "Start Date",
        "End Date",
        "Time Spend",
        "Current Watch Time Minutes"
    ];

    private static readonly string[] SeriesHeaders =
    [
        "Id",
        "Name",
        "Status",
        "Priority",
        "Release Date",
        "Genre",
        "Source",
        "Description",
        "TMDBId",
        "Episode Count",
        "Expected Minutes Per Episode",
        "Expected Watch Time Minutes",
        "Parent Series Id",
        "Image Url",
        "Rating",
        "Start Date",
        "End Date",
        "Time Spend",
        "Current Episode",
        "Current Watch Time Minutes"
    ];

    public async Task<byte[]> ExportGamesAsync(LuminaUser user)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var myGames = await context.MyGames
            .Where(g => g.LuminaUserId == user.Id)
            .Include(g => g.MyGameInfo)
            .Include(g => g.Game)
                .ThenInclude(g => g!.ExternalIds)
            .Include(g => g.Game)
                .ThenInclude(g => g!.Image)
            .OrderBy(g => g.Game!.Name)
            .ToListAsync();

        var myAnimes = await context.MyAnimes
            .Where(anime => anime.LuminaUserId == user.Id)
            .Include(anime => anime.Anime)
                .ThenInclude(anime => anime!.ExternalIds)
            .Include(anime => anime.Anime)
                .ThenInclude(anime => anime!.Image)
            .OrderBy(anime => anime.Anime!.Name)
            .ToListAsync();

        var myMovies = await context.MyMovies
            .Where(movie => movie.LuminaUserId == user.Id)
            .Include(movie => movie.Movie)
                .ThenInclude(movie => movie!.ExternalIds)
            .Include(movie => movie.Movie)
                .ThenInclude(movie => movie!.Image)
            .OrderBy(movie => movie.Movie!.Name)
            .ToListAsync();

        var mySeries = await context.MySeries
            .Where(series => series.LuminaUserId == user.Id)
            .Include(series => series.Series)
                .ThenInclude(series => series!.ExternalIds)
            .Include(series => series.Series)
                .ThenInclude(series => series!.Image)
            .OrderBy(series => series.Series!.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        AddGamesWorksheet(workbook, myGames);
        AddAnimesWorksheet(workbook, myAnimes);
        AddMoviesWorksheet(workbook, myMovies);
        AddSeriesWorksheet(workbook, mySeries);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddGamesWorksheet(XLWorkbook workbook, IReadOnlyCollection<MyGame> myGames)
    {
        var worksheet = CreateWorksheet(workbook, "Games", GameHeaders);
        var row = 2;

        foreach (var myGame in myGames)
        {
            var game = myGame.Game;
            if (game is null)
            {
                continue;
            }

            var column = 1;
            worksheet.Cell(row, column++).Value = game.Id;
            worksheet.Cell(row, column++).Value = game.Name;
            worksheet.Cell(row, column++).Value = FormatStatus(myGame.Status);
            if (myGame.Priority != 0)
            {
                worksheet.Cell(row, column).Value = myGame.Priority;
            }
            column++;
            SetDate(worksheet.Cell(row, column++), game.ReleaseDate);
            worksheet.Cell(row, column++).Value = FormatPlatforms(game.Platforms);
            worksheet.Cell(row, column++).Value = string.Join(", ", game.Genres);
            worksheet.Cell(row, column++).Value = game.Source;
            worksheet.Cell(row, column++).Value = game.Description;
            if (game.Playtime.HasValue)
            {
                worksheet.Cell(row, column).Value = game.Playtime.Value;
            }
            column++;
            worksheet.Cell(row, column++).Value = game.ExternalIds.GetExternalId(ExternalMediaProvider.Psn);
            worksheet.Cell(row, column++).Value = game.ExternalIds.GetExternalId(ExternalMediaProvider.Steam);
            worksheet.Cell(row, column++).Value = game.ExternalIds.GetExternalId(ExternalMediaProvider.Igdb);
            worksheet.Cell(row, column++).Value = game.ExternalIds.GetExternalId(ExternalMediaProvider.Rawg);
            if (game.ParentGameId.HasValue)
            {
                worksheet.Cell(row, column).Value = game.ParentGameId.Value;
            }
            column++;
            worksheet.Cell(row, column++).Value = game.Image?.Url;
            if (myGame.Rating.HasValue)
            {
                worksheet.Cell(row, column).Value = myGame.Rating.Value;
            }
            column++;
            SetDate(worksheet.Cell(row, column++), myGame.StartDate);
            SetDate(worksheet.Cell(row, column++), myGame.EndDate);
            if (myGame.TimeSpend.HasValue)
            {
                worksheet.Cell(row, column).Value = myGame.TimeSpend.Value;
            }
            column++;
            SetDate(worksheet.Cell(row, column++), myGame.MyGameInfo?.FirstPlayed);
            SetDate(worksheet.Cell(row, column++), myGame.MyGameInfo?.LastPlayed);
            if (myGame.MyGameInfo is not null)
            {
                worksheet.Cell(row, column).Value = myGame.MyGameInfo.TrackedHours;
            }
            column++;
            worksheet.Cell(row, column).Value = myGame.PersonalNotes;

            row++;
        }

        FinalizeWorksheet(worksheet);
    }

    private static void AddAnimesWorksheet(XLWorkbook workbook, IReadOnlyCollection<MyAnime> myAnimes)
    {
        var worksheet = CreateWorksheet(workbook, "Animes", AnimeHeaders);
        var row = 2;

        foreach (var myAnime in myAnimes)
        {
            var anime = myAnime.Anime;
            if (anime is null)
            {
                continue;
            }

            var column = WriteCommonLibraryColumns(worksheet, row, anime, myAnime.Status.ToString(), myAnime.Priority);
            worksheet.Cell(row, column++).Value = anime.ExternalIds.GetExternalId(ExternalMediaProvider.Anilist);
            worksheet.Cell(row, column++).Value = anime.ExternalIds.GetExternalId(ExternalMediaProvider.Mal);
            SetInt(worksheet.Cell(row, column++), anime.EpisodeCount);
            SetInt(worksheet.Cell(row, column++), anime.ExpectedWatchTimePerEpisodeMinutes);
            SetInt(worksheet.Cell(row, column++), anime.ExpectedWatchTimeMinutes);
            SetInt(worksheet.Cell(row, column++), anime.ParentAnimeId);
            worksheet.Cell(row, column++).Value = anime.Image?.Url;
            WriteCommonUserMediaColumns(worksheet, row, column, myAnime.Rating, myAnime.StartDate, myAnime.EndDate, myAnime.TimeSpend);
            column += 4;
            SetInt(worksheet.Cell(row, column++), myAnime.CurrentEpisode);
            SetInt(worksheet.Cell(row, column), myAnime.CurrentWatchTimeMinutes);
            row++;
        }

        FinalizeWorksheet(worksheet);
    }

    private static void AddMoviesWorksheet(XLWorkbook workbook, IReadOnlyCollection<MyMovie> myMovies)
    {
        var worksheet = CreateWorksheet(workbook, "Movies", MovieHeaders);
        var row = 2;

        foreach (var myMovie in myMovies)
        {
            var movie = myMovie.Movie;
            if (movie is null)
            {
                continue;
            }

            var column = WriteCommonLibraryColumns(worksheet, row, movie, myMovie.Status.ToString(), myMovie.Priority);
            worksheet.Cell(row, column++).Value = movie.ExternalIds.GetExternalId(ExternalMediaProvider.Tmdb);
            SetInt(worksheet.Cell(row, column++), movie.ExpectedWatchTimeMinutes);
            worksheet.Cell(row, column++).Value = movie.Image?.Url;
            WriteCommonUserMediaColumns(worksheet, row, column, myMovie.Rating, myMovie.StartDate, myMovie.EndDate, myMovie.TimeSpend);
            column += 4;
            SetInt(worksheet.Cell(row, column), myMovie.CurrentWatchTimeMinutes);
            row++;
        }

        FinalizeWorksheet(worksheet);
    }

    private static void AddSeriesWorksheet(XLWorkbook workbook, IReadOnlyCollection<MySeries> mySeries)
    {
        var worksheet = CreateWorksheet(workbook, "Series", SeriesHeaders);
        var row = 2;

        foreach (var mySeriesEntry in mySeries)
        {
            var series = mySeriesEntry.Series;
            if (series is null)
            {
                continue;
            }

            var column = WriteCommonLibraryColumns(worksheet, row, series, mySeriesEntry.Status.ToString(), mySeriesEntry.Priority);
            worksheet.Cell(row, column++).Value = series.ExternalIds.GetExternalId(ExternalMediaProvider.Tmdb);
            SetInt(worksheet.Cell(row, column++), series.EpisodeCount);
            SetInt(worksheet.Cell(row, column++), series.ExpectedWatchTimePerEpisodeMinutes);
            SetInt(worksheet.Cell(row, column++), series.ExpectedWatchTimeMinutes);
            SetInt(worksheet.Cell(row, column++), series.ParentSeriesId);
            worksheet.Cell(row, column++).Value = series.Image?.Url;
            WriteCommonUserMediaColumns(worksheet, row, column, mySeriesEntry.Rating, mySeriesEntry.StartDate, mySeriesEntry.EndDate, mySeriesEntry.TimeSpend);
            column += 4;
            SetInt(worksheet.Cell(row, column++), mySeriesEntry.CurrentEpisode);
            SetInt(worksheet.Cell(row, column), mySeriesEntry.CurrentWatchTimeMinutes);
            row++;
        }

        FinalizeWorksheet(worksheet);
    }

    private static IXLWorksheet CreateWorksheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers)
    {
        var worksheet = workbook.Worksheets.Add(name);

        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        var header = worksheet.Range(1, 1, 1, headers.Count);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAF7");
        return worksheet;
    }

    private static int WriteCommonLibraryColumns(
        IXLWorksheet worksheet,
        int row,
        Media media,
        string status,
        int priority)
    {
        var column = 1;
        worksheet.Cell(row, column++).Value = media.Id;
        worksheet.Cell(row, column++).Value = media.Name;
        worksheet.Cell(row, column++).Value = status;
        if (priority != 0)
        {
            worksheet.Cell(row, column).Value = priority;
        }
        column++;
        SetDate(worksheet.Cell(row, column++), media.ReleaseDate);
        worksheet.Cell(row, column++).Value = string.Join(", ", media.Genres);
        worksheet.Cell(row, column++).Value = media.Source;
        worksheet.Cell(row, column++).Value = media.Description;
        return column;
    }

    private static void WriteCommonUserMediaColumns(
        IXLWorksheet worksheet,
        int row,
        int column,
        short? rating,
        DateTime? startDate,
        DateTime? endDate,
        double? timeSpend)
    {
        if (rating.HasValue)
        {
            worksheet.Cell(row, column).Value = rating.Value;
        }
        column++;
        SetDate(worksheet.Cell(row, column++), startDate);
        SetDate(worksheet.Cell(row, column++), endDate);
        if (timeSpend.HasValue)
        {
            worksheet.Cell(row, column).Value = timeSpend.Value;
        }
    }

    private static void FinalizeWorksheet(IXLWorksheet worksheet)
    {
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);
    }

    private static void SetDate(IXLCell cell, DateTime? date)
    {
        if (!date.HasValue)
        {
            return;
        }

        cell.Value = date.Value;
        cell.Style.DateFormat.Format = "dd.MM.yyyy";
    }

    private static void SetInt(IXLCell cell, int? value)
    {
        if (value.HasValue)
        {
            cell.Value = value.Value;
        }
    }
}
