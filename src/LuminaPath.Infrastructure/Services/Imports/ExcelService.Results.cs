namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
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
}
