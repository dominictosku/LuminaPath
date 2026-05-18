using ClosedXML.Excel;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Helper;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LuminaPath.Infrastructure.Services
{
    public class ExcelService
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
            "Rating",
            "Start Date",
            "End Date",
            "Time Spend",
            "First Played",
            "Last Played",
            "Tracked Hours"
        ];

        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly GameImportPipeline _importPipeline;

        public ExcelService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            GameImportPipeline importPipeline)
        {
            _dbContextFactory = dbContextFactory;
            _importPipeline = importPipeline;
        }

        public async Task<byte[]> ExportGamesAsync(LuminaUser user)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var myGames = await context.MyGames
                .Where(g => g.LuminaUserId == user.Id)
                .Include(g => g.MyGameInfo)
                .Include(g => g.Game)
                    .ThenInclude(g => g!.ExternalIds)
                .OrderBy(g => g.Game!.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Games");

            for (var index = 0; index < GameHeaders.Length; index++)
            {
                worksheet.Cell(1, index + 1).Value = GameHeaders[index];
            }

            var header = worksheet.Range(1, 1, 1, GameHeaders.Length);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAF7");

            var row = 2;
            foreach (var myGame in myGames)
            {
                var game = myGame.Game;
                if (game is null)
                {
                    continue;
                }

                worksheet.Cell(row, 1).Value = game.Id;
                worksheet.Cell(row, 2).Value = game.Name;
                worksheet.Cell(row, 3).Value = FormatStatus(myGame.Status);
                if (myGame.Priority != 0)
                {
                    worksheet.Cell(row, 4).Value = myGame.Priority;
                }
                SetDate(worksheet.Cell(row, 5), game.ReleaseDate);
                worksheet.Cell(row, 6).Value = FormatPlatforms(game.Platforms);
                worksheet.Cell(row, 7).Value = string.Join(", ", game.Genres);
                worksheet.Cell(row, 8).Value = game.Source;
                worksheet.Cell(row, 9).Value = game.Description;
                if (game.Playtime.HasValue)
                {
                    worksheet.Cell(row, 10).Value = game.Playtime.Value;
                }
                worksheet.Cell(row, 11).Value = game.ExternalIds.GetExternalId(ExternalMediaProvider.Psn);
                if (myGame.Rating.HasValue)
                {
                    worksheet.Cell(row, 12).Value = myGame.Rating.Value;
                }
                SetDate(worksheet.Cell(row, 13), myGame.StartDate);
                SetDate(worksheet.Cell(row, 14), myGame.EndDate);
                if (myGame.TimeSpend.HasValue)
                {
                    worksheet.Cell(row, 15).Value = myGame.TimeSpend.Value;
                }
                SetDate(worksheet.Cell(row, 16), myGame.MyGameInfo?.FirstPlayed);
                SetDate(worksheet.Cell(row, 17), myGame.MyGameInfo?.LastPlayed);
                if (myGame.MyGameInfo is not null)
                {
                    worksheet.Cell(row, 18).Value = myGame.MyGameInfo.TrackedHours;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public Task<GameExcelImportResult> ImportGamesAsync(Stream stream, LuminaUser user)
        {
            return ImportGamesAsync(stream, user, fileName: null);
        }

        public async Task<GameExcelImportResult> ImportGamesAsync(Stream stream, LuminaUser user, string? fileName)
        {
            if (IsOdsWorkbook(stream, fileName))
            {
                return await ImportOdsGamesAsync(stream, user);
            }

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Games", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("The workbook does not contain a worksheet.");

            var headerMap = BuildHeaderMap(worksheet);
            if (!headerMap.ContainsKey("name"))
            {
                throw new InvalidOperationException("The workbook needs a 'Name' column.");
            }

            var result = new GameExcelImportResult();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var items = new List<GameImportItem>();

            for (var row = 2; row <= lastRow; row++)
            {
                var name = GetText(worksheet, row, headerMap, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                try
                {
                    items.Add(BuildImportItem(worksheet, row, headerMap, name));
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
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

        public Task<GameExcelPreviewResult> PreviewGamesAsync(Stream stream, LuminaUser user)
        {
            return PreviewGamesAsync(stream, user, fileName: null);
        }

        public async Task<GameExcelPreviewResult> PreviewGamesAsync(Stream stream, LuminaUser user, string? fileName)
        {
            if (IsOdsWorkbook(stream, fileName))
            {
                return await PreviewOdsGamesAsync(stream, user);
            }

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Games", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("The workbook does not contain a worksheet.");

            var headerMap = BuildHeaderMap(worksheet);
            if (!headerMap.ContainsKey("name"))
            {
                throw new InvalidOperationException("The workbook needs a 'Name' column.");
            }

            var result = new GameExcelPreviewResult();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var previewRows = new List<GameExcelPreviewRow>();
            var items = new List<GameImportItem>();

            for (var row = 2; row <= lastRow; row++)
            {
                var name = GetText(worksheet, row, headerMap, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var previewRow = new GameExcelPreviewRow
                {
                    RowNumber = row,
                    Name = name,
                    Platform = GetText(worksheet, row, headerMap, "platform") ?? string.Empty,
                    Status = GetText(worksheet, row, headerMap, "status") ?? string.Empty,
                    Source = GetText(worksheet, row, headerMap, "source") ?? "Excel",
                    PsnId = GetText(worksheet, row, headerMap, "psnid", "psn id", "psn") ?? string.Empty,
                    TrackedHours = GetDouble(worksheet, row, headerMap, "trackedhours", "playtimeinhours"),
                };

                try
                {
                    items.Add(BuildImportItem(worksheet, row, headerMap, name));
                }
                catch (Exception ex)
                {
                    previewRow.Error = ex.Message;
                    previewRow.ChangeType = "Error";
                    result.Errors.Add($"Row {row}: {ex.Message}");
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

        public class GameExcelImportResult
        {
            public int RowsImported { get; set; }
            public int CreatedGames { get; set; }
            public int UpdatedGames { get; set; }
            public int CreatedMyGames { get; set; }
            public int UpdatedMyGames { get; set; }
            public List<string> Errors { get; } = new();
        }

        public class GameExcelPreviewResult
        {
            public int RowsDetected { get; set; }
            public int DuplicateRows { get; set; }
            public int CreatedGames { get; set; }
            public int UpdatedGames { get; set; }
            public int CreatedMyGames { get; set; }
            public int UpdatedMyGames { get; set; }
            public List<string> Errors { get; } = new();
            public List<GameExcelPreviewRow> Rows { get; } = new();
        }

        public class GameExcelPreviewRow
        {
            public int RowNumber { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Platform { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string PsnId { get; set; } = string.Empty;
            public double? TrackedHours { get; set; }
            public string GameAction { get; set; } = string.Empty;
            public string LibraryAction { get; set; } = string.Empty;
            public string ChangeType { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }

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
            var psnId = GetText(worksheet, row, headerMap, "psnid", "psn id", "psn");
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
                ExternalProvider = string.IsNullOrWhiteSpace(psnId) ? null : ExternalMediaProvider.Psn,
                ExternalId = psnId,
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

        private static void SetDate(IXLCell cell, DateTime? date)
        {
            if (!date.HasValue)
            {
                return;
            }

            cell.Value = date.Value;
            cell.Style.DateFormat.Format = "dd.MM.yyyy";
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
                return new List<OdsRow>();
            }

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

        private static DateTime? ToUtcDate(DateTime? date)
        {
            if (!date.HasValue)
            {
                return null;
            }

            return UtcDateTime.Normalize(date);
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

        private static IEnumerable<string> SplitList(string value)
        {
            return value.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static string FormatStatus(GameStatus status)
        {
            return status switch
            {
                GameStatus.OnHold => "On-Hold",
                GameStatus.Playing => "In-Progress",
                GameStatus.StoryComplete => "Story Complete",
                GameStatus.MainGame => "Main Game",
                _ => status.ToString()
            };
        }

        private static GameStatus ParseStatus(string value)
        {
            var normalized = NormalizeToken(value);
            return normalized switch
            {
                "onhold" => GameStatus.OnHold,
                "planned" => GameStatus.Planned,
                "playing" or "inprogress" => GameStatus.Playing,
                "storycomplete" => GameStatus.StoryComplete,
                "completed" or "complete" => GameStatus.Completed,
                "maingame" => GameStatus.MainGame,
                _ => Enum.TryParse<GameStatus>(value, true, out var status) ? status : GameStatus.Planned
            };
        }

        private static string FormatPlatforms(Platforms platforms)
        {
            return platforms == 0
                ? string.Empty
                : string.Join(", ", Enum.GetValues<Platforms>().Where(platform => platforms.HasFlag(platform)));
        }

        private static Platforms ParsePlatforms(string value)
        {
            Platforms result = 0;

            foreach (var platform in SplitList(value))
            {
                var normalized = NormalizeToken(platform);
                result |= normalized switch
                {
                    "playstation4" or "ps4" => Platforms.Playstation4,
                    "playstation5" or "ps5" => Platforms.Playstation5,
                    "switch" or "nintendoswitch" => Platforms.Switch,
                    "pc" => Platforms.PC,
                    "xbox" => Platforms.XBOX,
                    _ => Enum.TryParse<Platforms>(platform, true, out var parsed) ? parsed : 0
                };
            }

            return result;
        }

        private static string NormalizeHeader(string value)
        {
            return NormalizeToken(value);
        }

        private static string NormalizeToken(string value)
        {
            return Regex.Replace(value ?? string.Empty, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
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
}
