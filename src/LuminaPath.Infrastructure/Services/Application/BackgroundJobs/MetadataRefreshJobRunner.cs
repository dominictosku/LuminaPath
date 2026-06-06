using LuminaPath.Infrastructure.Services.Application;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class MetadataRefreshJobRunner : IBackgroundJobRunner
{
    private readonly MetadataRefreshMaintenanceService _metadataRefreshService;
    private readonly string _mediaType;

    public MetadataRefreshJobRunner(
        MetadataRefreshMaintenanceService metadataRefreshService,
        string jobType,
        string mediaType)
    {
        _metadataRefreshService = metadataRefreshService;
        JobType = jobType;
        _mediaType = mediaType;
    }

    public string JobType { get; }

    public async Task<string?> RunAsync(string? payload, CancellationToken cancellationToken)
    {
        var result = await _metadataRefreshService.RefreshMissingAsync(_mediaType, cancellationToken);
        return $"{result.Message} Updated release dates: {result.UpdatedReleaseDates}; images: {result.UpdatedImages}; skipped: {result.Skipped}; failed: {result.Failed}.";
    }
}
