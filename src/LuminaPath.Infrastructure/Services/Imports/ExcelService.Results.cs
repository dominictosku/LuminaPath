namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    public class LibraryExcelImportResult
    {
        public int RowsImported { get; set; }
        public int DuplicateRows { get; set; }
        public int CreatedMedia { get; set; }
        public int UpdatedMedia { get; set; }
        public int CreatedLibraryItems { get; set; }
        public int UpdatedLibraryItems { get; set; }
        public List<string> Errors { get; } = new();
        public List<LibraryExcelSheetResult> Sheets { get; } = new();
    }

    public class LibraryExcelPreviewResult
    {
        public int RowsDetected { get; set; }
        public int DuplicateRows { get; set; }
        public int CreatedMedia { get; set; }
        public int UpdatedMedia { get; set; }
        public int CreatedLibraryItems { get; set; }
        public int UpdatedLibraryItems { get; set; }
        public List<string> Errors { get; } = new();
        public List<LibraryExcelSheetResult> Sheets { get; } = new();
        public List<LibraryExcelPreviewRow> Rows { get; } = new();
    }

    public class LibraryExcelSheetResult
    {
        public string SheetName { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int DuplicateRows { get; set; }
        public int CreatedMedia { get; set; }
        public int UpdatedMedia { get; set; }
        public int CreatedLibraryItems { get; set; }
        public int UpdatedLibraryItems { get; set; }
    }

    public class LibraryExcelPreviewRow
    {
        public string SheetName { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string MediaAction { get; set; } = string.Empty;
        public string LibraryAction { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
