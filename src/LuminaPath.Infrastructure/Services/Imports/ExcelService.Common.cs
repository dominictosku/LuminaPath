using LuminaPath.Core.Enums;
using LuminaPath.Infrastructure.Helper;
using System.Text.RegularExpressions;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    private static void ValidateWorkbookSize(Stream stream)
    {
        if (stream.CanSeek && stream.Length > MaxWorkbookBytes)
        {
            throw new InvalidOperationException($"Workbook is too large. Maximum allowed size is {MaxWorkbookBytes / (1024 * 1024)} MiB.");
        }
    }

    private static void ValidateImportRowCount(int dataRows)
    {
        if (dataRows > MaxImportRows)
        {
            throw new InvalidOperationException($"Workbook contains too many rows. Maximum allowed rows: {MaxImportRows}.");
        }
    }

    private static DateTime? ToUtcDate(DateTime? date)
    {
        if (!date.HasValue)
        {
            return null;
        }

        return UtcDateTime.Normalize(date);
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
}
