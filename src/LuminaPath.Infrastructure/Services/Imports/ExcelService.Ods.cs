using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    private async Task<GameExcelImportResult> ImportOdsGamesAsync(Stream stream, LuminaUser user)
    {
        var rows = ReadOdsRows(stream);
        var result = new GameExcelImportResult();
        var items = new List<GameImportItem>();

        foreach (var row in rows)
        {
            var name = GetOdsText(row, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            try
            {
                items.Add(BuildImportItem(row, name));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row.RowNumber}: {ex.Message}");
            }
        }

        var importResult = await _importPipeline.ImportAsync(user, items);
        result.RowsImported += importResult.RowsImported;
        result.CreatedGames += importResult.CreatedGames;
        result.UpdatedGames += importResult.UpdatedGames;
        result.CreatedMyGames += importResult.CreatedMyGames;
        result.UpdatedMyGames += importResult.UpdatedMyGames;
        result.Errors.AddRange(importResult.Errors);

        return result;
    }

    private async Task<GameExcelPreviewResult> PreviewOdsGamesAsync(Stream stream, LuminaUser user)
    {
        var rows = ReadOdsRows(stream);
        var result = new GameExcelPreviewResult();
        var previewRows = new List<GameExcelPreviewRow>();
        var items = new List<GameImportItem>();

        foreach (var row in rows)
        {
            var name = GetOdsText(row, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var previewRow = new GameExcelPreviewRow
            {
                RowNumber = row.RowNumber,
                Name = name,
                Platform = GetOdsText(row, "platform") ?? string.Empty,
                Status = GetOdsText(row, "status") ?? string.Empty,
                Source = GetOdsText(row, "source") ?? "ODS",
                PsnId = GetOdsText(row, "psnid", "psn id", "psn") ?? string.Empty,
                TrackedHours = GetOdsDouble(row, "trackedhours", "playtimeinhours"),
            };

            try
            {
                items.Add(BuildImportItem(row, name));
            }
            catch (Exception ex)
            {
                previewRow.Error = ex.Message;
                previewRow.ChangeType = "Error";
                result.Errors.Add($"Row {row.RowNumber}: {ex.Message}");
            }

            previewRows.Add(previewRow);
        }

        var pipelinePreview = await _importPipeline.PreviewAsync(user, items);
        result.RowsDetected = pipelinePreview.RowsDetected;
        result.DuplicateRows = pipelinePreview.DuplicateRows;
        result.CreatedGames = pipelinePreview.CreatedGames;
        result.UpdatedGames = pipelinePreview.UpdatedGames;
        result.CreatedMyGames = pipelinePreview.CreatedMyGames;
        result.UpdatedMyGames = pipelinePreview.UpdatedMyGames;
        result.Errors.AddRange(pipelinePreview.Errors);

        var pipelineRowsByNumber = pipelinePreview.Rows.ToDictionary(row => row.RowNumber);
        foreach (var previewRow in previewRows)
        {
            pipelineRowsByNumber.TryGetValue(previewRow.RowNumber, out var pipelineRow);
            previewRow.GameAction = pipelineRow?.GameAction ?? previewRow.GameAction;
            previewRow.LibraryAction = pipelineRow?.LibraryAction ?? previewRow.LibraryAction;
            previewRow.ChangeType = pipelineRow?.ChangeType ?? previewRow.ChangeType;
            previewRow.Error = string.IsNullOrWhiteSpace(previewRow.Error)
                ? pipelineRow?.Error ?? string.Empty
                : previewRow.Error;
            result.Rows.Add(previewRow);
        }

        return result;
    }

    private static GameImportItem BuildImportItem(OdsRow row, string name)
    {
        var psnId = GetOdsText(row, "psnid", "psn id", "psn");
        var platform = GetOdsText(row, "platform");
        var genres = GetOdsText(row, "genre", "genres");
        var status = GetOdsText(row, "status");

        return new GameImportItem
        {
            RowNumber = row.RowNumber,
            Name = name,
            Description = GetOdsText(row, "description"),
            Source = GetOdsText(row, "source") ?? "ODS",
            ReleaseDate = ToUtcDate(GetOdsDate(row, "releasedate")),
            Platforms = string.IsNullOrWhiteSpace(platform) ? 0 : ParsePlatforms(platform),
            Genres = string.IsNullOrWhiteSpace(genres) ? new List<string>() : SplitList(genres).ToList(),
            Playtime = GetOdsInt(row, "playtime", "estimatedplaytime"),
            ExternalProvider = string.IsNullOrWhiteSpace(psnId) ? null : ExternalMediaProvider.Psn,
            ExternalId = psnId,
            Status = string.IsNullOrWhiteSpace(status) ? GameStatus.Planned : ParseStatus(status),
            Priority = GetOdsInt(row, "priority") ?? 0,
            Rating = GetOdsShort(row, "rating"),
            StartDate = ToUtcDate(GetOdsDate(row, "startdate", "startedon")),
            EndDate = ToUtcDate(GetOdsDate(row, "enddate", "finishedon")),
            TimeSpend = GetOdsDouble(row, "timespend", "timespent"),
            FirstPlayed = ToUtcDate(GetOdsDate(row, "firstplayed")),
            LastPlayed = ToUtcDate(GetOdsDate(row, "lastplayed")),
            TrackedHours = GetOdsDouble(row, "trackedhours", "playtimeinhours"),
        };
    }

    private static bool IsOdsWorkbook(Stream stream, string? fileName)
    {
        if (fileName?.EndsWith(".ods", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (!stream.CanSeek)
        {
            return false;
        }

        var position = stream.Position;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return archive.GetEntry("content.xml") is not null
                && archive.GetEntry("mimetype") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        finally
        {
            stream.Position = position;
        }
    }

    private static List<OdsRow> ReadOdsRows(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var contentEntry = archive.GetEntry("content.xml")
            ?? throw new InvalidOperationException("The ODS file does not contain content.xml.");

        using var contentStream = contentEntry.Open();
        var document = XDocument.Load(contentStream);
        XNamespace tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
        XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
        XNamespace officeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

        var sheet = document.Descendants(tableNs + "table").FirstOrDefault()
            ?? throw new InvalidOperationException("The ODS file does not contain a worksheet.");

        var allRows = new List<List<string>>();
        foreach (var rowElement in sheet.Elements(tableNs + "table-row"))
        {
            var repeatRows = GetRepeatedCount(rowElement.Attribute(tableNs + "number-rows-repeated")?.Value);
            var cells = new List<string>();

            foreach (var cellElement in rowElement.Elements().Where(element =>
                element.Name == tableNs + "table-cell" || element.Name == tableNs + "covered-table-cell"))
            {
                var repeatColumns = GetRepeatedCount(cellElement.Attribute(tableNs + "number-columns-repeated")?.Value);
                var value = ReadOdsCell(cellElement, textNs, officeNs);
                for (var repeat = 0; repeat < repeatColumns; repeat++)
                {
                    cells.Add(value);
                }
            }

            if (cells.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            {
                for (var repeat = 0; repeat < repeatRows; repeat++)
                {
                    allRows.Add(cells);
                }
            }
        }

        if (allRows.Count == 0)
        {
            return [];
        }

        ValidateImportRowCount(allRows.Count - 1);

        var headerMap = new Dictionary<string, int>();
        for (var index = 0; index < allRows[0].Count; index++)
        {
            var header = NormalizeHeader(allRows[0][index]);
            if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
            {
                headerMap.Add(header, index);
            }
        }

        if (!headerMap.ContainsKey("name"))
        {
            throw new InvalidOperationException("The workbook needs a 'Name' column.");
        }

        return allRows
            .Skip(1)
            .Select((cells, index) => new OdsRow(index + 2, cells, headerMap))
            .ToList();
    }

    private static int GetRepeatedCount(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var repeated)
            ? Math.Clamp(repeated, 1, 1000)
            : 1;
    }

    private static string ReadOdsCell(XElement cellElement, XNamespace textNs, XNamespace officeNs)
    {
        var dateValue = cellElement.Attribute(officeNs + "date-value")?.Value;
        if (!string.IsNullOrWhiteSpace(dateValue))
        {
            return dateValue;
        }

        var value = cellElement.Attribute(officeNs + "value")?.Value
            ?? cellElement.Attribute(officeNs + "string-value")?.Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Join(" ", cellElement.Descendants(textNs + "p").Select(paragraph => paragraph.Value.Trim()))
            .Trim();
    }

    private static string? GetOdsText(OdsRow row, params string[] headers)
    {
        var value = row.Get(headers);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? GetOdsInt(OdsRow row, params string[] headers)
    {
        var value = GetOdsDouble(row, headers);
        return value.HasValue ? Convert.ToInt32(value.Value) : null;
    }

    private static short? GetOdsShort(OdsRow row, params string[] headers)
    {
        var value = GetOdsInt(row, headers);
        return value.HasValue ? Convert.ToInt16(value.Value) : null;
    }

    private static double? GetOdsDouble(OdsRow row, params string[] headers)
    {
        var text = row.Get(headers);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out number)
            ? number
            : null;
    }

    private static DateTime? GetOdsDate(OdsRow row, params string[] headers)
    {
        var text = row.Get(headers);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var formats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "M/d/yyyy" };
        return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date
            : null;
    }

    private sealed class OdsRow
    {
        private readonly List<string> _cells;
        private readonly Dictionary<string, int> _headerMap;

        public OdsRow(int rowNumber, List<string> cells, Dictionary<string, int> headerMap)
        {
            RowNumber = rowNumber;
            _cells = cells;
            _headerMap = headerMap;
        }

        public int RowNumber { get; }

        public string? Get(params string[] headers)
        {
            foreach (var header in headers.Select(NormalizeHeader))
            {
                if (_headerMap.TryGetValue(header, out var index) && index < _cells.Count)
                {
                    return _cells[index];
                }
            }

            return null;
        }
    }
}
