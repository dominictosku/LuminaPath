using ClosedXML.Excel;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    public Task<GameExcelImportResult> ImportGamesAsync(Stream stream, LuminaUser user)
    {
        return ImportGamesAsync(stream, user, fileName: null);
    }

    public async Task<GameExcelImportResult> ImportGamesAsync(Stream stream, LuminaUser user, string? fileName)
    {
        ValidateWorkbookSize(stream);

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
        ValidateImportRowCount(lastRow - 1);
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
        ValidateWorkbookSize(stream);

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
        ValidateImportRowCount(lastRow - 1);
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
}
