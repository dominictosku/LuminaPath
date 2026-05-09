using LuminaPath.Core.Enums;

namespace LuminaPath.Infrastructure.Services.Imports;

public sealed class GameImportItem
{
    public int? RowNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = "Import";

    public string? Description { get; set; }

    public List<string> Genres { get; set; } = new();

    public DateTime? ReleaseDate { get; set; }

    public Platforms Platforms { get; set; }

    public int? Playtime { get; set; }

    public ExternalMediaProvider? ExternalProvider { get; set; }

    public string? ExternalId { get; set; }

    public GameStatus Status { get; set; } = GameStatus.Planned;

    public int Priority { get; set; }

    public short? Rating { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public double? TimeSpend { get; set; }

    public DateTime? FirstPlayed { get; set; }

    public DateTime? LastPlayed { get; set; }

    public double? TrackedHours { get; set; }
}

public sealed class GameImportResult
{
    public int RowsImported { get; set; }

    public int CreatedGames { get; set; }

    public int UpdatedGames { get; set; }

    public int CreatedMyGames { get; set; }

    public int UpdatedMyGames { get; set; }

    public List<string> Errors { get; } = new();
}

public sealed class GameImportPreviewResult
{
    public int RowsDetected { get; set; }

    public int CreatedGames { get; set; }

    public int UpdatedGames { get; set; }

    public int CreatedMyGames { get; set; }

    public int UpdatedMyGames { get; set; }

    public List<GameImportPreviewItem> Rows { get; } = new();

    public List<string> Errors { get; } = new();
}

public sealed class GameImportPreviewItem
{
    public int RowNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public string GameAction { get; set; } = string.Empty;

    public string LibraryAction { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;
}
