using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services;

public sealed class ApplicationSettingsService
{
    public const string SteamApiKey = "Steam.ApiKey";
    public const string PsnBearerToken = "PSN.BearerToken";
    public const string MetadataProvider = "Metadata.Provider";
    public const string MetadataIgdbClientId = "Metadata.IgdbClientId";
    public const string MetadataIgdbClientSecret = "Metadata.IgdbClientSecret";
    public const string MetadataRawgApiKey = "Metadata.RawgApiKey";
    public const string BackgroundJobsScheduledJobsEnabled = "BackgroundJobs.ScheduledJobsEnabled";
    public const string BackgroundJobsBackupIntervalHours = "BackgroundJobs.BackupIntervalHours";
    public const string BackgroundJobsBackupRetentionCount = "BackgroundJobs.BackupRetentionCount";
    public const string BackgroundJobsJobHistoryRetentionDays = "BackgroundJobs.JobHistoryRetentionDays";
    public const string BackgroundJobsMaintenanceIntervalHours = "BackgroundJobs.MaintenanceIntervalHours";
    public const string BackgroundJobsOrphanedBlobCleanupIntervalHours = "BackgroundJobs.OrphanedBlobCleanupIntervalHours";
    public const string NewsEnabled = "News.Enabled";
    public const string NewsCustomRssUrl = "News.CustomRssUrl";
    public const string GoogleCalendarClientId = "GoogleCalendar.ClientId";
    public const string GoogleCalendarClientSecret = "GoogleCalendar.ClientSecret";
    public const string GoogleCalendarName = "GoogleCalendar.CalendarName";
    public const string AiChatEnabled = "AiChat.Enabled";
    public const string AiChatProvider = "AiChat.Provider";
    public const string AiChatMaxToolIterations = "AiChat.MaxToolIterations";
    public const string AiChatEnableMcpTools = "AiChat.EnableMcpTools";
    public const string AiChatEnableWriteTools = "AiChat.EnableWriteTools";
    public const string AnthropicApiKey = "Anthropic.ApiKey";
    public const string AnthropicModel = "Anthropic.Model";
    public const string AnthropicMaxTokens = "Anthropic.MaxTokens";
    public const string AnthropicBaseUrl = "Anthropic.BaseUrl";
    public const string AnthropicVersion = "Anthropic.AnthropicVersion";
    public const string OpenAiApiKey = "OpenAi.ApiKey";
    public const string OpenAiBaseUrl = "OpenAi.BaseUrl";
    public const string OpenAiModel = "OpenAi.Model";
    public const string OpenAiToolChoice = "OpenAi.ToolChoice";
    public const string OpenAiMaxTokens = "OpenAi.MaxTokens";

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        SteamApiKey,
        PsnBearerToken,
        MetadataIgdbClientSecret,
        MetadataRawgApiKey,
        GoogleCalendarClientSecret,
        AnthropicApiKey,
        OpenAiApiKey
    };

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly AuditLogService? _auditLog;

    public ApplicationSettingsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory)
        : this(dbContextFactory, null)
    {
    }

    public ApplicationSettingsService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, AuditLogService? auditLog)
    {
        _dbContextFactory = dbContextFactory;
        _auditLog = auditLog;
    }

    public async Task<string> GetSteamApiKeyAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(SteamApiKey, cancellationToken);
    }

    public async Task<bool> HasSteamApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetSteamApiKeyAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(value);
    }

    public Task SaveSteamApiKeyAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(SteamApiKey, value.Trim(), cancellationToken);
    }

    public async Task<string> GetPsnBearerTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(PsnBearerToken, cancellationToken);
    }

    public Task SavePsnBearerTokenAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(PsnBearerToken, value.Trim(), cancellationToken);
    }

    public async Task<GameMetadataSettings> GetGameMetadataSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == MetadataProvider
                || setting.Key == MetadataIgdbClientId
                || setting.Key == MetadataIgdbClientSecret
                || setting.Key == MetadataRawgApiKey)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return new GameMetadataSettings(
            settings.GetValueOrDefault(MetadataProvider) ?? string.Empty,
            settings.GetValueOrDefault(MetadataIgdbClientId) ?? string.Empty,
            settings.GetValueOrDefault(MetadataIgdbClientSecret) ?? string.Empty,
            settings.GetValueOrDefault(MetadataRawgApiKey) ?? string.Empty);
    }

    public async Task<string> GetGameMetadataProviderAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataProvider, cancellationToken);
    }

    public async Task<string> GetIgdbClientIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataIgdbClientId, cancellationToken);
    }

    public async Task<string> GetIgdbClientSecretAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataIgdbClientSecret, cancellationToken);
    }

    public async Task<string> GetRawgApiKeyAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(MetadataRawgApiKey, cancellationToken);
    }

    public async Task SaveGameMetadataSettingsAsync(GameMetadataSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(MetadataProvider, settings.Provider.Trim(), cancellationToken);
        await SaveValueAsync(MetadataIgdbClientId, settings.IgdbClientId.Trim(), cancellationToken);
        await SaveValueAsync(MetadataIgdbClientSecret, settings.IgdbClientSecret.Trim(), cancellationToken);
        await SaveValueAsync(MetadataRawgApiKey, settings.RawgApiKey.Trim(), cancellationToken);
    }

    public async Task<GoogleCalendarSettings> GetGoogleCalendarSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == GoogleCalendarClientId
                || setting.Key == GoogleCalendarClientSecret
                || setting.Key == GoogleCalendarName)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return new GoogleCalendarSettings(
            settings.GetValueOrDefault(GoogleCalendarClientId) ?? string.Empty,
            settings.GetValueOrDefault(GoogleCalendarClientSecret) ?? string.Empty,
            settings.GetValueOrDefault(GoogleCalendarName) ?? string.Empty);
    }

    public async Task SaveGoogleCalendarSettingsAsync(GoogleCalendarSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(GoogleCalendarClientId, settings.ClientId.Trim(), cancellationToken);
        await SaveValueAsync(GoogleCalendarClientSecret, settings.ClientSecret.Trim(), cancellationToken);
        await SaveValueAsync(GoogleCalendarName, settings.CalendarName.Trim(), cancellationToken);
    }

    public async Task<bool> GetNewsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(NewsEnabled, cancellationToken);
        return string.IsNullOrWhiteSpace(value) || bool.TryParse(value, out var enabled) && enabled;
    }

    public async Task<BackgroundJobStoredSettings> GetBackgroundJobSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == BackgroundJobsScheduledJobsEnabled
                || setting.Key == BackgroundJobsBackupIntervalHours
                || setting.Key == BackgroundJobsBackupRetentionCount
                || setting.Key == BackgroundJobsJobHistoryRetentionDays
                || setting.Key == BackgroundJobsMaintenanceIntervalHours
                || setting.Key == BackgroundJobsOrphanedBlobCleanupIntervalHours)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return new BackgroundJobStoredSettings(
            settings.GetValueOrDefault(BackgroundJobsScheduledJobsEnabled) ?? string.Empty,
            settings.GetValueOrDefault(BackgroundJobsBackupIntervalHours) ?? string.Empty,
            settings.GetValueOrDefault(BackgroundJobsBackupRetentionCount) ?? string.Empty,
            settings.GetValueOrDefault(BackgroundJobsJobHistoryRetentionDays) ?? string.Empty,
            settings.GetValueOrDefault(BackgroundJobsMaintenanceIntervalHours) ?? string.Empty,
            settings.GetValueOrDefault(BackgroundJobsOrphanedBlobCleanupIntervalHours) ?? string.Empty);
    }

    public async Task SaveBackgroundJobSettingsAsync(BackgroundJobStoredSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(BackgroundJobsScheduledJobsEnabled, settings.ScheduledJobsEnabled.Trim(), cancellationToken);
        await SaveValueAsync(BackgroundJobsBackupIntervalHours, settings.BackupIntervalHours.Trim(), cancellationToken);
        await SaveValueAsync(BackgroundJobsBackupRetentionCount, settings.BackupRetentionCount.Trim(), cancellationToken);
        await SaveValueAsync(BackgroundJobsJobHistoryRetentionDays, settings.JobHistoryRetentionDays.Trim(), cancellationToken);
        await SaveValueAsync(BackgroundJobsMaintenanceIntervalHours, settings.MaintenanceIntervalHours.Trim(), cancellationToken);
        await SaveValueAsync(BackgroundJobsOrphanedBlobCleanupIntervalHours, settings.OrphanedBlobCleanupIntervalHours.Trim(), cancellationToken);
    }

    public async Task<AiChatStoredSettings> GetAiChatSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == AiChatEnabled
                || setting.Key == AiChatProvider
                || setting.Key == AiChatMaxToolIterations
                || setting.Key == AiChatEnableMcpTools
                || setting.Key == AiChatEnableWriteTools
                || setting.Key == AnthropicApiKey
                || setting.Key == AnthropicModel
                || setting.Key == AnthropicMaxTokens
                || setting.Key == AnthropicBaseUrl
                || setting.Key == AnthropicVersion
                || setting.Key == OpenAiApiKey
                || setting.Key == OpenAiBaseUrl
                || setting.Key == OpenAiModel
                || setting.Key == OpenAiToolChoice
                || setting.Key == OpenAiMaxTokens)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        return new AiChatStoredSettings(
            settings.GetValueOrDefault(AiChatEnabled) ?? string.Empty,
            settings.GetValueOrDefault(AiChatProvider) ?? string.Empty,
            settings.GetValueOrDefault(AiChatMaxToolIterations) ?? string.Empty,
            settings.GetValueOrDefault(AiChatEnableMcpTools) ?? string.Empty,
            settings.GetValueOrDefault(AiChatEnableWriteTools) ?? string.Empty,
            settings.GetValueOrDefault(AnthropicApiKey) ?? string.Empty,
            settings.GetValueOrDefault(AnthropicModel) ?? string.Empty,
            settings.GetValueOrDefault(AnthropicMaxTokens) ?? string.Empty,
            settings.GetValueOrDefault(AnthropicBaseUrl) ?? string.Empty,
            settings.GetValueOrDefault(AnthropicVersion) ?? string.Empty,
            settings.GetValueOrDefault(OpenAiApiKey) ?? string.Empty,
            settings.GetValueOrDefault(OpenAiBaseUrl) ?? string.Empty,
            settings.GetValueOrDefault(OpenAiModel) ?? string.Empty,
            settings.GetValueOrDefault(OpenAiToolChoice) ?? string.Empty,
            settings.GetValueOrDefault(OpenAiMaxTokens) ?? string.Empty);
    }

    public async Task SaveAiChatSettingsAsync(AiChatStoredSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(AiChatEnabled, settings.Enabled.Trim(), cancellationToken);
        await SaveValueAsync(AiChatProvider, settings.Provider.Trim(), cancellationToken);
        await SaveValueAsync(AiChatMaxToolIterations, settings.MaxToolIterations.Trim(), cancellationToken);
        await SaveValueAsync(AiChatEnableMcpTools, settings.EnableMcpTools.Trim(), cancellationToken);
        await SaveValueAsync(AiChatEnableWriteTools, settings.EnableWriteTools.Trim(), cancellationToken);
        await SaveValueAsync(AnthropicApiKey, settings.AnthropicApiKey.Trim(), cancellationToken);
        await SaveValueAsync(AnthropicModel, settings.AnthropicModel.Trim(), cancellationToken);
        await SaveValueAsync(AnthropicMaxTokens, settings.AnthropicMaxTokens.Trim(), cancellationToken);
        await SaveValueAsync(AnthropicBaseUrl, settings.AnthropicBaseUrl.Trim(), cancellationToken);
        await SaveValueAsync(AnthropicVersion, settings.AnthropicVersion.Trim(), cancellationToken);
        await SaveValueAsync(OpenAiApiKey, settings.OpenAiApiKey.Trim(), cancellationToken);
        await SaveValueAsync(OpenAiBaseUrl, settings.OpenAiBaseUrl.Trim(), cancellationToken);
        await SaveValueAsync(OpenAiModel, settings.OpenAiModel.Trim(), cancellationToken);
        await SaveValueAsync(OpenAiToolChoice, settings.OpenAiToolChoice.Trim(), cancellationToken);
        await SaveValueAsync(OpenAiMaxTokens, settings.OpenAiMaxTokens.Trim(), cancellationToken);
    }

    public Task SaveNewsEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(NewsEnabled, enabled.ToString(), cancellationToken);
    }

    public async Task<string> GetNewsCustomRssUrlAsync(CancellationToken cancellationToken = default)
    {
        return await GetValueAsync(NewsCustomRssUrl, cancellationToken);
    }

    public Task SaveNewsCustomRssUrlAsync(string value, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync(NewsCustomRssUrl, value.Trim(), cancellationToken);
    }

    private async Task<string> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ApplicationSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken)
            ?? string.Empty;
    }

    private async Task SaveValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.ApplicationSettings
            .FirstOrDefaultAsync(item => item.Key == key, cancellationToken);

        var oldValue = setting?.Value;
        if (string.Equals(oldValue, value, StringComparison.Ordinal))
        {
            return;
        }

        if (setting is null)
        {
            context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        await AuditSettingChangedAsync(key, oldValue, value, cancellationToken);
    }

    private async Task AuditSettingChangedAsync(string key, string? oldValue, string newValue, CancellationToken cancellationToken)
    {
        if (_auditLog is null)
        {
            return;
        }

        var isSensitive = SensitiveKeys.Contains(key);
        await _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Admin,
            Action = AuditActions.ApplicationSettingChanged,
            Outcome = AuditOutcomes.Success,
            TargetType = "ApplicationSetting",
            TargetId = key,
            TargetName = key,
            Changes = AuditLogService.Changes((
                "Value",
                isSensitive ? AuditLogService.ValueState(oldValue) : oldValue,
                isSensitive ? AuditLogService.ValueState(newValue) : newValue)),
            Metadata = new
            {
                sensitive = isSensitive,
                source = "ApplicationSettings"
            }
        }, cancellationToken);
    }
}

public sealed record GameMetadataSettings(
    string Provider,
    string IgdbClientId,
    string IgdbClientSecret,
    string RawgApiKey);

public sealed record GoogleCalendarSettings(
    string ClientId,
    string ClientSecret,
    string CalendarName);

public sealed record BackgroundJobStoredSettings(
    string ScheduledJobsEnabled,
    string BackupIntervalHours,
    string BackupRetentionCount,
    string JobHistoryRetentionDays,
    string MaintenanceIntervalHours,
    string OrphanedBlobCleanupIntervalHours);

public sealed record AiChatStoredSettings(
    string Enabled,
    string Provider,
    string MaxToolIterations,
    string EnableMcpTools,
    string EnableWriteTools,
    string AnthropicApiKey,
    string AnthropicModel,
    string AnthropicMaxTokens,
    string AnthropicBaseUrl,
    string AnthropicVersion,
    string OpenAiApiKey,
    string OpenAiBaseUrl,
    string OpenAiModel,
    string OpenAiToolChoice,
    string OpenAiMaxTokens);
