using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    private static readonly string[] LibraryWorksheetNames = ["Games", "Animes", "Movies", "Series"];

    public async Task<LibraryExcelImportResult> ImportLibraryWorkbookAsync(Stream stream, LuminaUser user)
    {
        ValidateWorkbookSize(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var workbook = new XLWorkbook(stream);
        var result = new LibraryExcelImportResult();

        await ImportGameWorksheetAsync(workbook, user, result);
        await ImportMediaWorksheetAsync(workbook, user, result, CreateAnimeImportSpec());
        await ImportMediaWorksheetAsync(workbook, user, result, CreateMovieImportSpec());
        await ImportMediaWorksheetAsync(workbook, user, result, CreateSeriesImportSpec());

        if (!result.Sheets.Any())
        {
            throw new InvalidOperationException("The workbook does not contain a supported library worksheet.");
        }

        return result;
    }

    public async Task<LibraryExcelPreviewResult> PreviewLibraryWorkbookAsync(Stream stream, LuminaUser user)
    {
        ValidateWorkbookSize(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var workbook = new XLWorkbook(stream);
        var result = new LibraryExcelPreviewResult();

        await PreviewGameWorksheetAsync(workbook, user, result);
        await PreviewMediaWorksheetAsync(workbook, user, result, CreateAnimeImportSpec());
        await PreviewMediaWorksheetAsync(workbook, user, result, CreateMovieImportSpec());
        await PreviewMediaWorksheetAsync(workbook, user, result, CreateSeriesImportSpec());

        if (!result.Sheets.Any())
        {
            throw new InvalidOperationException("The workbook does not contain a supported library worksheet.");
        }

        return result;
    }

    private async Task ImportGameWorksheetAsync(
        XLWorkbook workbook,
        LuminaUser user,
        LibraryExcelImportResult result)
    {
        var worksheet = FindLibraryWorksheet(workbook, "Games", allowFallback: true);
        if (worksheet is null)
        {
            return;
        }

        var items = ReadGameItems(worksheet, result.Errors);
        var importResult = await _importPipeline.ImportAsync(user, items);
        var sheet = new LibraryExcelSheetResult
        {
            SheetName = worksheet.Name,
            MediaType = "Game",
            Rows = importResult.RowsImported,
            CreatedMedia = importResult.CreatedGames,
            UpdatedMedia = importResult.UpdatedGames,
            CreatedLibraryItems = importResult.CreatedMyGames,
            UpdatedLibraryItems = importResult.UpdatedMyGames
        };

        result.RowsImported += importResult.RowsImported;
        result.CreatedMedia += importResult.CreatedGames;
        result.UpdatedMedia += importResult.UpdatedGames;
        result.CreatedLibraryItems += importResult.CreatedMyGames;
        result.UpdatedLibraryItems += importResult.UpdatedMyGames;
        result.Errors.AddRange(importResult.Errors.Select(error => $"{worksheet.Name}: {error}"));
        result.Sheets.Add(sheet);
    }

    private async Task PreviewGameWorksheetAsync(
        XLWorkbook workbook,
        LuminaUser user,
        LibraryExcelPreviewResult result)
    {
        var worksheet = FindLibraryWorksheet(workbook, "Games", allowFallback: true);
        if (worksheet is null)
        {
            return;
        }

        var previewRows = new List<LibraryExcelPreviewRow>();
        var items = ReadGameItems(worksheet, result.Errors, previewRows);
        var pipelinePreview = await _importPipeline.PreviewAsync(user, items);
        var sheet = new LibraryExcelSheetResult
        {
            SheetName = worksheet.Name,
            MediaType = "Game",
            Rows = pipelinePreview.RowsDetected,
            DuplicateRows = pipelinePreview.DuplicateRows,
            CreatedMedia = pipelinePreview.CreatedGames,
            UpdatedMedia = pipelinePreview.UpdatedGames,
            CreatedLibraryItems = pipelinePreview.CreatedMyGames,
            UpdatedLibraryItems = pipelinePreview.UpdatedMyGames
        };

        result.RowsDetected += pipelinePreview.RowsDetected;
        result.DuplicateRows += pipelinePreview.DuplicateRows;
        result.CreatedMedia += pipelinePreview.CreatedGames;
        result.UpdatedMedia += pipelinePreview.UpdatedGames;
        result.CreatedLibraryItems += pipelinePreview.CreatedMyGames;
        result.UpdatedLibraryItems += pipelinePreview.UpdatedMyGames;
        result.Errors.AddRange(pipelinePreview.Errors.Select(error => $"{worksheet.Name}: {error}"));
        result.Sheets.Add(sheet);

        var pipelineRowsByNumber = pipelinePreview.Rows.ToDictionary(row => row.RowNumber);
        foreach (var previewRow in previewRows)
        {
            pipelineRowsByNumber.TryGetValue(previewRow.RowNumber, out var pipelineRow);
            previewRow.MediaAction = pipelineRow?.GameAction ?? previewRow.MediaAction;
            previewRow.LibraryAction = pipelineRow?.LibraryAction ?? previewRow.LibraryAction;
            previewRow.ChangeType = pipelineRow?.ChangeType ?? previewRow.ChangeType;
            previewRow.Error = string.IsNullOrWhiteSpace(previewRow.Error)
                ? pipelineRow?.Error ?? string.Empty
                : previewRow.Error;
            result.Rows.Add(previewRow);
        }
    }

    private static List<GameImportItem> ReadGameItems(
        IXLWorksheet worksheet,
        List<string> errors,
        List<LibraryExcelPreviewRow>? previewRows = null)
    {
        var headerMap = BuildHeaderMap(worksheet);
        if (!headerMap.ContainsKey("name"))
        {
            errors.Add($"{worksheet.Name}: The worksheet needs a 'Name' column.");
            return new List<GameImportItem>();
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        ValidateImportRowCount(lastRow - 1);
        var items = new List<GameImportItem>();

        for (var row = 2; row <= lastRow; row++)
        {
            var name = GetText(worksheet, row, headerMap, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var externalIds = GetGameExternalIds(worksheet, row, headerMap);
            var previewRow = new LibraryExcelPreviewRow
            {
                SheetName = worksheet.Name,
                MediaType = "Game",
                RowNumber = row,
                Name = name,
                Status = GetText(worksheet, row, headerMap, "status") ?? string.Empty,
                Source = GetText(worksheet, row, headerMap, "source") ?? "Excel",
                ExternalId = FormatExternalId(externalIds)
            };

            try
            {
                items.Add(BuildImportItem(worksheet, row, headerMap, name));
            }
            catch (Exception ex)
            {
                previewRow.Error = ex.Message;
                previewRow.ChangeType = "Error";
                errors.Add($"{worksheet.Name} row {row}: {ex.Message}");
            }

            previewRows?.Add(previewRow);
        }

        return items;
    }

    private async Task ImportMediaWorksheetAsync<TMedia, TMyMedia>(
        XLWorkbook workbook,
        LuminaUser user,
        LibraryExcelImportResult result,
        LibrarySheetImportSpec<TMedia, TMyMedia> spec)
        where TMedia : Media
        where TMyMedia : MyMedia
    {
        var worksheet = FindLibraryWorksheet(workbook, spec.SheetName);
        if (worksheet is null)
        {
            return;
        }

        var items = ReadMediaItems(worksheet, spec, result.Errors);
        var sheet = new LibraryExcelSheetResult
        {
            SheetName = worksheet.Name,
            MediaType = spec.MediaType
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var mediaItems = await spec.LoadMedia(context).ToListAsync();
        var names = new HashSet<string>(mediaItems.Select(media => media.Name), StringComparer.OrdinalIgnoreCase);
        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var importedByExportedId = new Dictionary<int, TMedia>();
        var parentLinks = new List<(TMedia Media, int ParentExportedId)>();

        foreach (var item in items)
        {
            try
            {
                if (!seenRows.Add(GetLibraryImportKey(item)))
                {
                    result.DuplicateRows++;
                    sheet.DuplicateRows++;
                    result.Errors.Add($"{worksheet.Name} row {item.RowNumber}: duplicate import row skipped.");
                    continue;
                }

                var media = FindMedia(mediaItems, item);
                if (media is null)
                {
                    media = spec.CreateMedia(ResolveUniqueMediaName(item.Name.Trim(), names, item));
                    spec.ApplyMediaValues(media, item);
                    mediaItems.Add(media);
                    context.Set<TMedia>().Add(media);
                    result.CreatedMedia++;
                    sheet.CreatedMedia++;
                }
                else
                {
                    spec.ApplyMediaValues(media, item);
                    result.UpdatedMedia++;
                    sheet.UpdatedMedia++;
                }

                EnsureExternalIds(media, item.ExternalIds);

                var libraryItems = spec.EnsureLibraryItems(media);
                var libraryItem = libraryItems.FirstOrDefault(entry => entry.LuminaUserId == user.Id);
                if (libraryItem is null)
                {
                    libraryItem = spec.CreateLibraryItem(media);
                    libraryItem.LuminaUserId = user.Id;
                    libraryItems.Add(libraryItem);
                    context.Set<TMyMedia>().Add(libraryItem);
                    result.CreatedLibraryItems++;
                    sheet.CreatedLibraryItems++;
                }
                else
                {
                    result.UpdatedLibraryItems++;
                    sheet.UpdatedLibraryItems++;
                }

                spec.ApplyLibraryValues(libraryItem, item, media);

                if (item.ExportedId.HasValue)
                {
                    importedByExportedId[item.ExportedId.Value] = media;
                }

                if (item.ParentMediaId.HasValue && spec.SetParent is not null)
                {
                    parentLinks.Add((media, item.ParentMediaId.Value));
                }

                result.RowsImported++;
                sheet.Rows++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{worksheet.Name} row {item.RowNumber}: {ex.Message}");
            }
        }

        foreach (var (media, parentExportedId) in parentLinks)
        {
            if (importedByExportedId.TryGetValue(parentExportedId, out var parent))
            {
                spec.SetParent?.Invoke(media, parent);
            }
        }

        if (sheet.Rows > 0 || sheet.DuplicateRows > 0)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                result.Errors.Add($"{worksheet.Name}: Database save failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        result.Sheets.Add(sheet);
    }

    private async Task PreviewMediaWorksheetAsync<TMedia, TMyMedia>(
        XLWorkbook workbook,
        LuminaUser user,
        LibraryExcelPreviewResult result,
        LibrarySheetImportSpec<TMedia, TMyMedia> spec)
        where TMedia : Media
        where TMyMedia : MyMedia
    {
        var worksheet = FindLibraryWorksheet(workbook, spec.SheetName);
        if (worksheet is null)
        {
            return;
        }

        var items = ReadMediaItems(worksheet, spec, result.Errors);
        var sheet = new LibraryExcelSheetResult
        {
            SheetName = worksheet.Name,
            MediaType = spec.MediaType
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var mediaItems = await spec.LoadMedia(context).AsNoTracking().ToListAsync();
        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var previewRow = new LibraryExcelPreviewRow
            {
                SheetName = worksheet.Name,
                MediaType = spec.MediaType,
                RowNumber = item.RowNumber,
                Name = item.Name,
                Status = item.Status,
                Source = item.Source,
                ExternalId = FormatExternalId(item.ExternalIds)
            };

            try
            {
                if (!seenRows.Add(GetLibraryImportKey(item)))
                {
                    previewRow.MediaAction = "Duplicate";
                    previewRow.LibraryAction = "Skip";
                    previewRow.ChangeType = "Duplicate";
                    result.DuplicateRows++;
                    sheet.DuplicateRows++;
                    result.RowsDetected++;
                    sheet.Rows++;
                    result.Rows.Add(previewRow);
                    continue;
                }

                var media = FindMedia(mediaItems, item);
                if (media is null)
                {
                    previewRow.MediaAction = "Create";
                    previewRow.LibraryAction = "Create";
                    previewRow.ChangeType = "New";
                    result.CreatedMedia++;
                    result.CreatedLibraryItems++;
                    sheet.CreatedMedia++;
                    sheet.CreatedLibraryItems++;
                }
                else
                {
                    previewRow.MediaAction = "Update";
                    previewRow.ChangeType = "Updated";
                    result.UpdatedMedia++;
                    sheet.UpdatedMedia++;

                    if (spec.GetLibraryItems(media).Any(entry => entry.LuminaUserId == user.Id))
                    {
                        previewRow.LibraryAction = "Update";
                        result.UpdatedLibraryItems++;
                        sheet.UpdatedLibraryItems++;
                    }
                    else
                    {
                        previewRow.LibraryAction = "Create";
                        result.CreatedLibraryItems++;
                        sheet.CreatedLibraryItems++;
                    }
                }

                result.RowsDetected++;
                sheet.Rows++;
            }
            catch (Exception ex)
            {
                previewRow.Error = ex.Message;
                previewRow.ChangeType = "Error";
                result.Errors.Add($"{worksheet.Name} row {item.RowNumber}: {ex.Message}");
            }

            result.Rows.Add(previewRow);
        }

        result.Sheets.Add(sheet);
    }

    private static List<LibraryWorkbookItem> ReadMediaItems<TMedia, TMyMedia>(
        IXLWorksheet worksheet,
        LibrarySheetImportSpec<TMedia, TMyMedia> spec,
        List<string> errors)
        where TMedia : Media
        where TMyMedia : MyMedia
    {
        var headerMap = BuildHeaderMap(worksheet);
        if (!headerMap.ContainsKey("name"))
        {
            errors.Add($"{worksheet.Name}: The worksheet needs a 'Name' column.");
            return new List<LibraryWorkbookItem>();
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        ValidateImportRowCount(lastRow - 1);
        var items = new List<LibraryWorkbookItem>();

        for (var row = 2; row <= lastRow; row++)
        {
            var name = GetText(worksheet, row, headerMap, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            try
            {
                items.Add(spec.BuildItem(worksheet, row, headerMap, name));
            }
            catch (Exception ex)
            {
                errors.Add($"{worksheet.Name} row {row}: {ex.Message}");
            }
        }

        return items;
    }

    private static IXLWorksheet? FindLibraryWorksheet(
        XLWorkbook workbook,
        string sheetName,
        bool allowFallback = false)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet =>
            sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
        if (worksheet is not null || !allowFallback)
        {
            return worksheet;
        }

        var hasKnownSheet = workbook.Worksheets.Any(sheet =>
            LibraryWorksheetNames.Any(knownSheet =>
                knownSheet.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase)));
        return hasKnownSheet ? null : workbook.Worksheets.FirstOrDefault();
    }

    private static LibraryWorkbookItem BuildAnimeItem(
        IXLWorksheet worksheet,
        int row,
        Dictionary<string, int> headerMap,
        string name)
    {
        var item = BuildCommonMediaItem(worksheet, row, headerMap, name, "Anime");
        AddExternalId(item.ExternalIds, ExternalMediaProvider.Anilist, GetText(worksheet, row, headerMap, "anilistid", "ani list id", "anilist"));
        AddExternalId(item.ExternalIds, ExternalMediaProvider.Mal, GetText(worksheet, row, headerMap, "malid", "mal id", "mal"));
        item.EpisodeCount = GetInt(worksheet, row, headerMap, "episodecount", "episodes");
        item.ExpectedMinutesPerEpisode = GetInt(worksheet, row, headerMap, "expectedminutesperepisode", "expectedwatchtimeperepisodeminutes");
        item.ExpectedWatchTimeMinutes = GetInt(worksheet, row, headerMap, "expectedwatchtimeminutes");
        item.ParentMediaId = GetInt(worksheet, row, headerMap, "parentanimeid");
        item.CurrentEpisode = GetInt(worksheet, row, headerMap, "currentepisode");
        item.CurrentWatchTimeMinutes = GetInt(worksheet, row, headerMap, "currentwatchtimeminutes");
        return item;
    }

    private static LibraryWorkbookItem BuildMovieItem(
        IXLWorksheet worksheet,
        int row,
        Dictionary<string, int> headerMap,
        string name)
    {
        var item = BuildCommonMediaItem(worksheet, row, headerMap, name, "Movie");
        AddExternalId(item.ExternalIds, ExternalMediaProvider.Tmdb, GetText(worksheet, row, headerMap, "tmdbid", "tmdb id", "tmdb"));
        item.ExpectedWatchTimeMinutes = GetInt(worksheet, row, headerMap, "expectedwatchtimeminutes");
        item.CurrentWatchTimeMinutes = GetInt(worksheet, row, headerMap, "currentwatchtimeminutes");
        return item;
    }

    private static LibraryWorkbookItem BuildSeriesItem(
        IXLWorksheet worksheet,
        int row,
        Dictionary<string, int> headerMap,
        string name)
    {
        var item = BuildCommonMediaItem(worksheet, row, headerMap, name, "Series");
        AddExternalId(item.ExternalIds, ExternalMediaProvider.Tmdb, GetText(worksheet, row, headerMap, "tmdbid", "tmdb id", "tmdb"));
        item.EpisodeCount = GetInt(worksheet, row, headerMap, "episodecount", "episodes");
        item.ExpectedMinutesPerEpisode = GetInt(worksheet, row, headerMap, "expectedminutesperepisode", "expectedwatchtimeperepisodeminutes");
        item.ExpectedWatchTimeMinutes = GetInt(worksheet, row, headerMap, "expectedwatchtimeminutes");
        item.ParentMediaId = GetInt(worksheet, row, headerMap, "parentseriesid");
        item.CurrentEpisode = GetInt(worksheet, row, headerMap, "currentepisode");
        item.CurrentWatchTimeMinutes = GetInt(worksheet, row, headerMap, "currentwatchtimeminutes");
        return item;
    }

    private static LibraryWorkbookItem BuildCommonMediaItem(
        IXLWorksheet worksheet,
        int row,
        Dictionary<string, int> headerMap,
        string name,
        string mediaType)
    {
        var genres = GetText(worksheet, row, headerMap, "genre", "genres");

        return new LibraryWorkbookItem
        {
            MediaType = mediaType,
            RowNumber = row,
            ExportedId = GetInt(worksheet, row, headerMap, "id"),
            Name = name,
            Status = GetText(worksheet, row, headerMap, "status") ?? MediaStatus.Planned.ToString(),
            Priority = GetInt(worksheet, row, headerMap, "priority") ?? 0,
            ReleaseDate = ToUtcDate(GetDate(worksheet, row, headerMap, "releasedate")),
            Genres = string.IsNullOrWhiteSpace(genres) ? new List<string>() : SplitList(genres).ToList(),
            Source = GetText(worksheet, row, headerMap, "source") ?? "Excel",
            Description = GetText(worksheet, row, headerMap, "description"),
            Rating = GetShort(worksheet, row, headerMap, "rating"),
            StartDate = ToUtcDate(GetDate(worksheet, row, headerMap, "startdate", "startedon")),
            EndDate = ToUtcDate(GetDate(worksheet, row, headerMap, "enddate", "finishedon")),
            TimeSpend = GetDouble(worksheet, row, headerMap, "timespend", "timespent")
        };
    }

    private static LibrarySheetImportSpec<Anime, MyAnime> CreateAnimeImportSpec()
    {
        return new LibrarySheetImportSpec<Anime, MyAnime>(
            "Animes",
            "Anime",
            BuildAnimeItem,
            context => context.Animes
                .Include(anime => anime.ExternalIds)
                .Include(anime => anime.MyAnimes)
                .AsSplitQuery(),
            name => new Anime { Name = name },
            ApplyAnimeValues,
            anime => anime.MyAnimes is null ? Enumerable.Empty<MyAnime>() : anime.MyAnimes,
            anime => anime.MyAnimes ??= new List<MyAnime>(),
            anime => new MyAnime { Anime = anime },
            ApplyMyAnimeValues,
            (anime, parent) => anime.ParentAnime = parent);
    }

    private static LibrarySheetImportSpec<Movie, MyMovie> CreateMovieImportSpec()
    {
        return new LibrarySheetImportSpec<Movie, MyMovie>(
            "Movies",
            "Movie",
            BuildMovieItem,
            context => context.Movies
                .Include(movie => movie.ExternalIds)
                .Include(movie => movie.MyMovies)
                .AsSplitQuery(),
            name => new Movie { Name = name },
            ApplyMovieValues,
            movie => movie.MyMovies is null ? Enumerable.Empty<MyMovie>() : movie.MyMovies,
            movie => movie.MyMovies ??= new List<MyMovie>(),
            movie => new MyMovie { Movie = movie },
            ApplyMyMovieValues,
            setParent: null);
    }

    private static LibrarySheetImportSpec<Series, MySeries> CreateSeriesImportSpec()
    {
        return new LibrarySheetImportSpec<Series, MySeries>(
            "Series",
            "Series",
            BuildSeriesItem,
            context => context.Series
                .Include(series => series.ExternalIds)
                .Include(series => series.MySeries)
                .AsSplitQuery(),
            name => new Series { Name = name },
            ApplySeriesValues,
            series => series.MySeries is null ? Enumerable.Empty<MySeries>() : series.MySeries,
            series => series.MySeries ??= new List<MySeries>(),
            series => new MySeries { Series = series },
            ApplyMySeriesValues,
            (series, parent) => series.ParentSeries = parent);
    }

    private static void ApplyCommonMediaValues(Media media, LibraryWorkbookItem item)
    {
        media.Description = item.Description ?? media.Description;
        media.Source = string.IsNullOrWhiteSpace(item.Source) ? media.Source : item.Source;
        media.ReleaseDate = item.ReleaseDate ?? media.ReleaseDate;

        if (item.Genres.Count > 0)
        {
            media.Genres = item.Genres;
        }
    }

    private static void ApplyAnimeValues(Anime anime, LibraryWorkbookItem item)
    {
        ApplyCommonMediaValues(anime, item);
        anime.EpisodeCount = item.EpisodeCount ?? anime.EpisodeCount;
        anime.ExpectedWatchTimePerEpisodeMinutes =
            ResolveMinutesPerEpisode(item, anime.ExpectedWatchTimePerEpisodeMinutes);
    }

    private static void ApplyMovieValues(Movie movie, LibraryWorkbookItem item)
    {
        ApplyCommonMediaValues(movie, item);
        movie.ExpectedWatchTimeMinutes = item.ExpectedWatchTimeMinutes ?? movie.ExpectedWatchTimeMinutes;
    }

    private static void ApplySeriesValues(Series series, LibraryWorkbookItem item)
    {
        ApplyCommonMediaValues(series, item);
        series.EpisodeCount = item.EpisodeCount ?? series.EpisodeCount;
        series.ExpectedWatchTimePerEpisodeMinutes =
            ResolveMinutesPerEpisode(item, series.ExpectedWatchTimePerEpisodeMinutes);
    }

    private static int? ResolveMinutesPerEpisode(LibraryWorkbookItem item, int? currentValue)
    {
        if (item.ExpectedMinutesPerEpisode.HasValue)
        {
            return item.ExpectedMinutesPerEpisode.Value;
        }

        return item.ExpectedWatchTimeMinutes.HasValue && item.EpisodeCount is > 0
            ? Math.Max(1, item.ExpectedWatchTimeMinutes.Value / item.EpisodeCount.Value)
            : currentValue;
    }

    private static void ApplyCommonLibraryValues(MyMedia libraryItem, LibraryWorkbookItem item)
    {
        libraryItem.Priority = item.Priority;
        libraryItem.Rating = item.Rating ?? libraryItem.Rating;
        libraryItem.StartDate = item.StartDate ?? libraryItem.StartDate;
        libraryItem.EndDate = item.EndDate ?? libraryItem.EndDate;
        libraryItem.TimeSpend = item.TimeSpend ?? libraryItem.TimeSpend;
    }

    private static void ApplyMyAnimeValues(MyAnime myAnime, LibraryWorkbookItem item, Anime anime)
    {
        myAnime.Status = ParseMediaStatus(item.Status);
        ApplyCommonLibraryValues(myAnime, item);
        myAnime.CurrentEpisode = item.CurrentEpisode ?? myAnime.CurrentEpisode;

        if (item.CurrentWatchTimeMinutes.HasValue)
        {
            myAnime.CurrentWatchTimeMinutes = item.CurrentWatchTimeMinutes.Value;
        }
        else if (item.CurrentEpisode.HasValue)
        {
            myAnime.RecalculateWatchTime(anime);
        }
    }

    private static void ApplyMyMovieValues(MyMovie myMovie, LibraryWorkbookItem item, Movie movie)
    {
        myMovie.Status = ParseMediaStatus(item.Status);
        ApplyCommonLibraryValues(myMovie, item);
        myMovie.CurrentWatchTimeMinutes = item.CurrentWatchTimeMinutes ?? myMovie.CurrentWatchTimeMinutes;
    }

    private static void ApplyMySeriesValues(MySeries mySeries, LibraryWorkbookItem item, Series series)
    {
        mySeries.Status = ParseMediaStatus(item.Status);
        ApplyCommonLibraryValues(mySeries, item);
        mySeries.CurrentEpisode = item.CurrentEpisode ?? mySeries.CurrentEpisode;

        if (item.CurrentWatchTimeMinutes.HasValue)
        {
            mySeries.CurrentWatchTimeMinutes = item.CurrentWatchTimeMinutes.Value;
        }
        else if (item.CurrentEpisode.HasValue)
        {
            mySeries.RecalculateWatchTime(series);
        }
    }

    private static TMedia? FindMedia<TMedia>(IEnumerable<TMedia> mediaItems, LibraryWorkbookItem item)
        where TMedia : Media
    {
        foreach (var candidateExternalId in item.ExternalIds)
        {
            var media = mediaItems.FirstOrDefault(mediaItem =>
                mediaItem.ExternalIds.Any(externalId =>
                    externalId.Provider == candidateExternalId.Key
                    && externalId.ExternalId.Equals(candidateExternalId.Value, StringComparison.OrdinalIgnoreCase)));

            if (media is not null)
            {
                return media;
            }
        }

        return mediaItems.FirstOrDefault(media => media.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureExternalIds(Media media, IReadOnlyDictionary<ExternalMediaProvider, string> externalIds)
    {
        foreach (var candidateExternalId in externalIds)
        {
            if (media.ExternalIds.Any(externalId =>
                    externalId.Provider == candidateExternalId.Key
                    && externalId.ExternalId.Equals(candidateExternalId.Value, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            media.ExternalIds.Add(new MediaExternalId
            {
                Provider = candidateExternalId.Key,
                ExternalId = candidateExternalId.Value
            });
        }
    }

    private static string GetLibraryImportKey(LibraryWorkbookItem item)
    {
        var externalId = GetPrimaryExternalId(item.ExternalIds);
        return externalId.HasValue
            ? $"{item.MediaType}:{externalId.Value.Key}:{externalId.Value.Value}"
            : $"{item.MediaType}:name:{item.Name.Trim()}";
    }

    private static string ResolveUniqueMediaName(string desired, HashSet<string> names, LibraryWorkbookItem item)
    {
        if (!names.Contains(desired))
        {
            names.Add(desired);
            return desired;
        }

        var externalId = GetPrimaryExternalId(item.ExternalIds);
        var suffix = externalId.HasValue
            ? $"{externalId.Value.Key} {externalId.Value.Value}"
            : "Import";
        var resolved = $"{desired} ({suffix})";
        names.Add(resolved);
        return resolved;
    }

    private sealed class LibraryWorkbookItem
    {
        public string MediaType { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public int? ExportedId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Source { get; set; } = "Excel";
        public string? Description { get; set; }
        public Dictionary<ExternalMediaProvider, string> ExternalIds { get; } = new();
        public short? Rating { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? TimeSpend { get; set; }
        public int? EpisodeCount { get; set; }
        public int? ExpectedMinutesPerEpisode { get; set; }
        public int? ExpectedWatchTimeMinutes { get; set; }
        public int? ParentMediaId { get; set; }
        public int? CurrentEpisode { get; set; }
        public int? CurrentWatchTimeMinutes { get; set; }
    }

    private sealed class LibrarySheetImportSpec<TMedia, TMyMedia>
        where TMedia : Media
        where TMyMedia : MyMedia
    {
        public LibrarySheetImportSpec(
            string sheetName,
            string mediaType,
            Func<IXLWorksheet, int, Dictionary<string, int>, string, LibraryWorkbookItem> buildItem,
            Func<LuminaPathDbContext, IQueryable<TMedia>> loadMedia,
            Func<string, TMedia> createMedia,
            Action<TMedia, LibraryWorkbookItem> applyMediaValues,
            Func<TMedia, IEnumerable<TMyMedia>> getLibraryItems,
            Func<TMedia, ICollection<TMyMedia>> ensureLibraryItems,
            Func<TMedia, TMyMedia> createLibraryItem,
            Action<TMyMedia, LibraryWorkbookItem, TMedia> applyLibraryValues,
            Action<TMedia, TMedia>? setParent)
        {
            SheetName = sheetName;
            MediaType = mediaType;
            BuildItem = buildItem;
            LoadMedia = loadMedia;
            CreateMedia = createMedia;
            ApplyMediaValues = applyMediaValues;
            GetLibraryItems = getLibraryItems;
            EnsureLibraryItems = ensureLibraryItems;
            CreateLibraryItem = createLibraryItem;
            ApplyLibraryValues = applyLibraryValues;
            SetParent = setParent;
        }

        public string SheetName { get; }
        public string MediaType { get; }
        public Func<IXLWorksheet, int, Dictionary<string, int>, string, LibraryWorkbookItem> BuildItem { get; }
        public Func<LuminaPathDbContext, IQueryable<TMedia>> LoadMedia { get; }
        public Func<string, TMedia> CreateMedia { get; }
        public Action<TMedia, LibraryWorkbookItem> ApplyMediaValues { get; }
        public Func<TMedia, IEnumerable<TMyMedia>> GetLibraryItems { get; }
        public Func<TMedia, ICollection<TMyMedia>> EnsureLibraryItems { get; }
        public Func<TMedia, TMyMedia> CreateLibraryItem { get; }
        public Action<TMyMedia, LibraryWorkbookItem, TMedia> ApplyLibraryValues { get; }
        public Action<TMedia, TMedia>? SetParent { get; }
    }
}
