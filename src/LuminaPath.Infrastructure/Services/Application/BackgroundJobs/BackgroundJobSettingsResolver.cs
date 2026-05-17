using LuminaPath.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Application.BackgroundJobs;

public sealed class BackgroundJobSettingsResolver
{
    private readonly BackgroundJobOptions _options;
    private readonly ApplicationSettingsService _settings;

    public BackgroundJobSettingsResolver(IOptions<BackgroundJobOptions> options, ApplicationSettingsService settings)
    {
        _options = options.Value;
        _settings = settings;
    }

    public async Task<BackgroundJobRuntimeSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetBackgroundJobSettingsAsync(cancellationToken);
        return new BackgroundJobRuntimeSettings(
            ParseBool(stored.ScheduledBackupsEnabled, _options.ScheduledBackupsEnabled),
            ParseInt(stored.BackupIntervalHours, _options.BackupIntervalHours, 1, 24 * 30),
            ParseInt(stored.BackupRetentionCount, _options.BackupRetentionCount, 1, 500),
            ParseInt(stored.JobHistoryRetentionDays, _options.JobHistoryRetentionDays, 1, 3650),
            Math.Clamp(_options.MaintenanceIntervalHours, 1, 24 * 30));
    }

    private static bool ParseBool(string value, bool fallback)
    {
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback, int min, int max)
    {
        return int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : Math.Clamp(fallback, min, max);
    }
}

public sealed record BackgroundJobRuntimeSettings(
    bool ScheduledBackupsEnabled,
    int BackupIntervalHours,
    int BackupRetentionCount,
    int JobHistoryRetentionDays,
    int MaintenanceIntervalHours);
