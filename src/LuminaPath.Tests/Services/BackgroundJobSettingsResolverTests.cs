using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.Application.BackgroundJobs;
using Microsoft.Extensions.Options;
using Test.Utilities;

namespace Test.Services;

public class BackgroundJobSettingsResolverTests
{
    [Fact]
    public async Task GetAsync_UsesOptions_WhenNoSettingsAreStored()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var resolver = CreateResolver(options, new BackgroundJobOptions
        {
            ScheduledJobsEnabled = true,
            BackupIntervalHours = 12,
            BackupRetentionCount = 5,
            JobHistoryRetentionDays = 14,
            MaintenanceIntervalHours = 3
        });

        var settings = await resolver.GetAsync();

        Assert.True(settings.ScheduledJobsEnabled);
        Assert.Equal(12, settings.BackupIntervalHours);
        Assert.Equal(5, settings.BackupRetentionCount);
        Assert.Equal(14, settings.JobHistoryRetentionDays);
        Assert.Equal(3, settings.MaintenanceIntervalHours);
    }

    [Fact]
    public async Task GetAsync_UsesStoredSettings_WhenPresent()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        await using (var context = new LuminaPathDbContext(options))
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsScheduledJobsEnabled, Value = "true" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsBackupIntervalHours, Value = "48" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsBackupRetentionCount, Value = "7" },
                new ApplicationSetting { Key = ApplicationSettingsService.BackgroundJobsJobHistoryRetentionDays, Value = "90" });
            await context.SaveChangesAsync();
        }

        var resolver = CreateResolver(options, new BackgroundJobOptions());

        var settings = await resolver.GetAsync();

        Assert.True(settings.ScheduledJobsEnabled);
        Assert.Equal(48, settings.BackupIntervalHours);
        Assert.Equal(7, settings.BackupRetentionCount);
        Assert.Equal(90, settings.JobHistoryRetentionDays);
    }

    private static BackgroundJobSettingsResolver CreateResolver(
        Microsoft.EntityFrameworkCore.DbContextOptions<LuminaPathDbContext> options,
        BackgroundJobOptions jobOptions)
    {
        var settingsService = new ApplicationSettingsService(new TestDbContextFactory(options));
        return new BackgroundJobSettingsResolver(Options.Create(jobOptions), settingsService);
    }
}
