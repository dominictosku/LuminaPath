using ClosedXML.Excel;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Extensions;
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
        "Rating",
        "Start Date",
        "End Date",
        "Time Spend",
        "First Played",
        "Last Played",
        "Tracked Hours"
    ];

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

    private static void SetDate(IXLCell cell, DateTime? date)
    {
        if (!date.HasValue)
        {
            return;
        }

        cell.Value = date.Value;
        cell.Style.DateFormat.Format = "dd.MM.yyyy";
    }
}
