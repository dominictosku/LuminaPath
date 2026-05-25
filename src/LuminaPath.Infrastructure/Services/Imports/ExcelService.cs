using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public partial class ExcelService
{
    public const long MaxWorkbookBytes = 10 * 1024 * 1024;
    public const int MaxImportRows = 5_000;

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly GameImportPipeline _importPipeline;

    public ExcelService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        GameImportPipeline importPipeline)
    {
        _dbContextFactory = dbContextFactory;
        _importPipeline = importPipeline;
    }
}
