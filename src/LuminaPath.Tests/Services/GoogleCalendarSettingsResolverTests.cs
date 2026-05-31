using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services;
using LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Test.Utilities;

namespace LuminaPath.Tests.Services;

public class GoogleCalendarSettingsResolverTests
{
    [Fact]
    public async Task GetAsync_WithNoStoredSettings_FallsBackToOptions()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var resolver = NewResolver(options, new GoogleCalendarOptions
        {
            ClientId = "env-id",
            ClientSecret = "env-secret",
            CalendarName = "EnvCalendar",
        });

        var effective = await resolver.GetAsync(CancellationToken.None);

        Assert.Equal("env-id", effective.ClientId);
        Assert.Equal("env-secret", effective.ClientSecret);
        Assert.Equal("EnvCalendar", effective.CalendarName);
        Assert.True(effective.IsConfigured);
    }

    [Fact]
    public async Task GetAsync_WithStoredSettings_OverridesOptions()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var settings = new ApplicationSettingsService(new TestDbContextFactory(options));
        await settings.SaveGoogleCalendarSettingsAsync(
            new GoogleCalendarSettings("db-id", "db-secret", "DbCalendar"));

        var resolver = NewResolver(options, new GoogleCalendarOptions
        {
            ClientId = "env-id",
            ClientSecret = "env-secret",
            CalendarName = "EnvCalendar",
        });

        var effective = await resolver.GetAsync(CancellationToken.None);

        Assert.Equal("db-id", effective.ClientId);
        Assert.Equal("db-secret", effective.ClientSecret);
        Assert.Equal("DbCalendar", effective.CalendarName);
        Assert.True(effective.IsConfigured);
    }

    [Fact]
    public async Task GetAsync_WithNothingConfigured_IsNotConfigured()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var resolver = NewResolver(options, new GoogleCalendarOptions());

        var effective = await resolver.GetAsync(CancellationToken.None);

        Assert.False(effective.IsConfigured);
        // CalendarName always has the option default even when unconfigured.
        Assert.Equal("LuminaPath", effective.CalendarName);
    }

    [Fact]
    public async Task GetAsync_WithStoredClientIdButNoSecret_FallsBackToOptionSecretPerField()
    {
        var options = Test.Utilities.DbContext.TestDbContextOptions();
        var settings = new ApplicationSettingsService(new TestDbContextFactory(options));
        // Only the client id is stored; the secret is left blank.
        await settings.SaveGoogleCalendarSettingsAsync(
            new GoogleCalendarSettings("db-id", "", ""));

        var resolver = NewResolver(options, new GoogleCalendarOptions
        {
            ClientSecret = "env-secret",
        });

        var effective = await resolver.GetAsync(CancellationToken.None);

        Assert.Equal("db-id", effective.ClientId);
        Assert.Equal("env-secret", effective.ClientSecret);
        Assert.True(effective.IsConfigured);
    }

    private static GoogleCalendarSettingsResolver NewResolver(
        DbContextOptions<LuminaPathDbContext> options,
        GoogleCalendarOptions calendarOptions)
        => new(
            Options.Create(calendarOptions),
            new ApplicationSettingsService(new TestDbContextFactory(options)));
}
