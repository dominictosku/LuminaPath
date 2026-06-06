using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Services.Imports;
using System.Globalization;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>();
        var lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(worksheet.Cell(1, column).GetString());
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
            {
                map.Add(header, column);
            }
        }

        return map;
    }

    private static GameImportItem BuildImportItem(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, string name)
    {
        var externalIds = GetGameExternalIds(worksheet, row, headerMap);
        var primaryExternalId = GetPrimaryExternalId(externalIds);
        var platform = GetText(worksheet, row, headerMap, "Platform", "platform");
        var genres = GetText(worksheet, row, headerMap, "genre", "genres");
        var status = GetText(worksheet, row, headerMap, "status");

        return new GameImportItem
        {
            RowNumber = row,
            Name = name,
            Description = GetText(worksheet, row, headerMap, "description"),
            Source = GetText(worksheet, row, headerMap, "source") ?? "Excel",
            ReleaseDate = ToUtcDate(GetDate(worksheet, row, headerMap, "releasedate")),
            Platforms = string.IsNullOrWhiteSpace(platform) ? 0 : ParsePlatforms(platform),
            Genres = string.IsNullOrWhiteSpace(genres) ? new List<string>() : SplitList(genres).ToList(),
            Playtime = GetInt(worksheet, row, headerMap, "playtime", "estimatedplaytime"),
            ExternalProvider = primaryExternalId?.Key,
            ExternalId = primaryExternalId?.Value,
            ExternalIds = externalIds,
            Status = string.IsNullOrWhiteSpace(status) ? GameStatus.Planned : ParseStatus(status),
            Priority = GetInt(worksheet, row, headerMap, "priority", "Priority") ?? 0,
            Rating = GetShort(worksheet, row, headerMap, "rating"),
            StartDate = ToUtcDate(GetDate(worksheet, row, headerMap, "startdate", "startedon")),
            EndDate = ToUtcDate(GetDate(worksheet, row, headerMap, "enddate", "finishedon")),
            TimeSpend = GetDouble(worksheet, row, headerMap, "timespend", "timespent"),
            FirstPlayed = ToUtcDate(GetDate(worksheet, row, headerMap, "firstplayed")),
            LastPlayed = ToUtcDate(GetDate(worksheet, row, headerMap, "lastplayed")),
            TrackedHours = GetDouble(worksheet, row, headerMap, "trackedhours", "playtimeinhours"),
        };
    }

    private static Dictionary<ExternalMediaProvider, string> GetGameExternalIds(
        IXLWorksheet worksheet,
        int row,
        Dictionary<string, int> headerMap)
    {
        var externalIds = new Dictionary<ExternalMediaProvider, string>();
        AddExternalId(externalIds, ExternalMediaProvider.Psn, GetText(worksheet, row, headerMap, "psnid", "psn id", "psn"));
        AddExternalId(externalIds, ExternalMediaProvider.Steam, GetText(worksheet, row, headerMap, "steamid", "steam id", "steam"));
        AddExternalId(externalIds, ExternalMediaProvider.Igdb, GetText(worksheet, row, headerMap, "igdbid", "igdb id", "igdb"));
        AddExternalId(externalIds, ExternalMediaProvider.Rawg, GetText(worksheet, row, headerMap, "rawgid", "rawg id", "rawg"));
        return externalIds;
    }

    private static string? GetText(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        var cell = GetCell(worksheet, row, headerMap, headers);
        var value = cell?.GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? GetInt(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        var value = GetDouble(worksheet, row, headerMap, headers);
        return value.HasValue ? Convert.ToInt32(value.Value) : null;
    }

    private static short? GetShort(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        var value = GetInt(worksheet, row, headerMap, headers);
        return value.HasValue ? Convert.ToInt16(value.Value) : null;
    }

    private static double? GetDouble(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        var cell = GetCell(worksheet, row, headerMap, headers);
        if (cell is null || cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<double>(out var number))
        {
            return number;
        }

        var text = cell.GetFormattedString();
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number)
            || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out number)
            ? number
            : null;
    }

    private static DateTime? GetDate(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        var cell = GetCell(worksheet, row, headerMap, headers);
        if (cell is null || cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var date))
        {
            return date;
        }

        if (cell.TryGetValue<double>(out var serial))
        {
            return DateTime.FromOADate(serial);
        }

        var text = cell.GetFormattedString();
        var formats = new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "yyyy-MM-dd", "M/d/yyyy" };
        return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date
            : null;
    }

    private static IXLCell? GetCell(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, params string[] headers)
    {
        foreach (var header in headers.Select(NormalizeHeader))
        {
            if (headerMap.TryGetValue(header, out var column))
            {
                return worksheet.Cell(row, column);
            }
        }

        return null;
    }
}
