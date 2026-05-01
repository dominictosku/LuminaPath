using ClosedXML.Excel;
using LuminaPath.Core.Entities;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

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
            "Plattform",
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

        public ExcelService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<byte[]> ExportGamesAsync(LuminaUser user)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var myGames = await context.MyGames
                .Where(g => g.LuminaUserId == user.Id)
                .Include(g => g.MyGameInfo)
                .Include(g => g.Game)
                    .ThenInclude(g => g!.GameInfo)
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
                if (myGame.Priortiy != 0)
                {
                    worksheet.Cell(row, 4).Value = myGame.Priortiy;
                }
                SetDate(worksheet.Cell(row, 5), game.ReleaseDate);
                worksheet.Cell(row, 6).Value = FormatPlatforms(game.Plattforms);
                worksheet.Cell(row, 7).Value = string.Join(", ", game.Genres);
                worksheet.Cell(row, 8).Value = game.Source;
                worksheet.Cell(row, 9).Value = game.Description;
                if (game.Playtime.HasValue)
                {
                    worksheet.Cell(row, 10).Value = game.Playtime.Value;
                }
                worksheet.Cell(row, 11).Value = game.GameInfo?.PsnId;
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

        public async Task<GameExcelImportResult> ImportGamesAsync(Stream stream, LuminaUser user)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Games", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("The workbook does not contain a worksheet.");

            var headerMap = BuildHeaderMap(worksheet);
            if (!headerMap.ContainsKey("name"))
            {
                throw new InvalidOperationException("The workbook needs a 'Name' column.");
            }

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var games = await context.Games
                .Include(g => g.GameInfo)
                .Include(g => g.MyGames!)
                    .ThenInclude(m => m.MyGameInfo)
                .ToListAsync();

            var result = new GameExcelImportResult();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var row = 2; row <= lastRow; row++)
            {
                var name = GetText(worksheet, row, headerMap, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                try
                {
                    var psnId = GetText(worksheet, row, headerMap, "psnid", "psn id", "psn");
                    var game = FindGame(games, name, psnId);
                    var isNewGame = game is null;

                    if (game is null)
                    {
                        game = new Game { Name = name, Source = "Excel" };
                        games.Add(game);
                        context.Games.Add(game);
                        result.CreatedGames++;
                    }
                    else
                    {
                        result.UpdatedGames++;
                    }

                    ApplyGameValues(game, worksheet, row, headerMap, psnId);

                    game.MyGames ??= new List<MyGame>();
                    var myGame = game.MyGames.FirstOrDefault(g => g.LuminaUserId == user.Id);
                    if (myGame is null)
                    {
                        myGame = new MyGame
                        {
                            Game = game,
                            LuminaUserId = user.Id
                        };
                        game.MyGames.Add(myGame);
                        context.MyGames.Add(myGame);
                        result.CreatedMyGames++;
                    }
                    else
                    {
                        result.UpdatedMyGames++;
                    }

                    ApplyMyGameValues(myGame, worksheet, row, headerMap);

                    if (isNewGame && string.IsNullOrWhiteSpace(game.Source))
                    {
                        game.Source = "Excel";
                    }

                    result.RowsImported++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }

            await context.SaveChangesAsync();
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

        private static Game? FindGame(IEnumerable<Game> games, string name, string? psnId)
        {
            if (!string.IsNullOrWhiteSpace(psnId))
            {
                var byPsnId = games.FirstOrDefault(g => g.GameInfo?.PsnId?.Equals(psnId, StringComparison.OrdinalIgnoreCase) == true);
                if (byPsnId is not null)
                {
                    return byPsnId;
                }
            }

            return games.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyGameValues(Game game, IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, string? psnId)
        {
            game.Name = GetText(worksheet, row, headerMap, "name") ?? game.Name;
            game.Description = GetText(worksheet, row, headerMap, "description") ?? game.Description;
            game.Source = GetText(worksheet, row, headerMap, "source") ?? game.Source;
            game.ReleaseDate = GetDate(worksheet, row, headerMap, "releasedate") ?? game.ReleaseDate;
            game.Playtime = GetInt(worksheet, row, headerMap, "playtime", "estimatedplaytime") ?? game.Playtime;

            var platform = GetText(worksheet, row, headerMap, "plattform", "platform");
            if (!string.IsNullOrWhiteSpace(platform))
            {
                game.Plattforms = ParsePlatforms(platform);
            }

            var genres = GetText(worksheet, row, headerMap, "genre", "genres");
            if (!string.IsNullOrWhiteSpace(genres))
            {
                game.Genres = SplitList(genres).ToList();
            }

            if (!string.IsNullOrWhiteSpace(psnId))
            {
                game.GameInfo ??= new GameInfo();
                game.GameInfo.PsnId = psnId;
            }
        }

        private static void ApplyMyGameValues(MyGame myGame, IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap)
        {
            var status = GetText(worksheet, row, headerMap, "status");
            if (!string.IsNullOrWhiteSpace(status))
            {
                myGame.Status = ParseStatus(status);
            }

            myGame.Priortiy = GetInt(worksheet, row, headerMap, "priority", "priortiy") ?? myGame.Priortiy;
            myGame.Rating = GetShort(worksheet, row, headerMap, "rating") ?? myGame.Rating;
            myGame.StartDate = GetDate(worksheet, row, headerMap, "startdate", "startedon") ?? myGame.StartDate;
            myGame.EndDate = GetDate(worksheet, row, headerMap, "enddate", "finishedon") ?? myGame.EndDate;
            myGame.TimeSpend = GetDouble(worksheet, row, headerMap, "timespend", "timespent") ?? myGame.TimeSpend;

            var firstPlayed = GetDate(worksheet, row, headerMap, "firstplayed");
            var lastPlayed = GetDate(worksheet, row, headerMap, "lastplayed");
            var trackedHours = GetDouble(worksheet, row, headerMap, "trackedhours", "playtimeinhours");

            if (firstPlayed.HasValue || lastPlayed.HasValue || trackedHours.HasValue)
            {
                myGame.MyGameInfo ??= new MyGameInfo();
                myGame.MyGameInfo.FirstPlayed = firstPlayed ?? myGame.MyGameInfo.FirstPlayed;
                myGame.MyGameInfo.LastPlayed = lastPlayed ?? myGame.MyGameInfo.LastPlayed;
                myGame.MyGameInfo.TrackedHours = trackedHours ?? myGame.MyGameInfo.TrackedHours;
            }
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

        private static string FormatPlatforms(Plattforms platforms)
        {
            return platforms == 0
                ? string.Empty
                : string.Join(", ", Enum.GetValues<Plattforms>().Where(platform => platforms.HasFlag(platform)));
        }

        private static Plattforms ParsePlatforms(string value)
        {
            Plattforms result = 0;

            foreach (var platform in SplitList(value))
            {
                var normalized = NormalizeToken(platform);
                result |= normalized switch
                {
                    "playstation4" or "ps4" => Plattforms.Playstation4,
                    "playstation5" or "ps5" => Plattforms.Playstation5,
                    "switch" or "nintendoswitch" => Plattforms.Switch,
                    "pc" => Plattforms.PC,
                    "xbox" => Plattforms.XBOX,
                    _ => Enum.TryParse<Plattforms>(platform, true, out var parsed) ? parsed : 0
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
    }
}
