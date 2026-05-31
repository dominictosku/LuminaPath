using LuminaPath.Core.Models.ThirdParty;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Manages the Google Calendar account link: builds the consent URL with a
/// signed, time-limited state bound to the user; completes the OAuth code
/// exchange and stores the encrypted refresh token; reports status; and
/// disconnects (best-effort token revoke + row delete).
/// </summary>
public sealed class GoogleCalendarConnectionService
{
    private const string Provider = GoogleCalendarSyncService.Provider;
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
    private readonly IGoogleOAuthClient _oauth;
    private readonly ICalendarTokenProtector _tokenProtector;
    private readonly ITimeLimitedDataProtector _stateProtector;
    private readonly Func<DateTime> _utcNow;

    public GoogleCalendarConnectionService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IGoogleOAuthClient oauth,
        ICalendarTokenProtector tokenProtector,
        IDataProtectionProvider dataProtectionProvider)
        : this(dbContextFactory, oauth, tokenProtector, dataProtectionProvider, () => DateTime.UtcNow)
    {
    }

    public GoogleCalendarConnectionService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        IGoogleOAuthClient oauth,
        ICalendarTokenProtector tokenProtector,
        IDataProtectionProvider dataProtectionProvider,
        Func<DateTime> utcNow)
    {
        _dbContextFactory = dbContextFactory;
        _oauth = oauth;
        _tokenProtector = tokenProtector;
        _stateProtector = dataProtectionProvider
            .CreateProtector("GoogleCalendar.OAuthState")
            .ToTimeLimitedDataProtector();
        _utcNow = utcNow;
    }

    /// <summary>
    /// Consent URL carrying a signed state that encodes the user id and the
    /// SPA/Blazor path to return to after the callback (so the shared callback
    /// can serve both the Angular settings page and the Blazor manage page).
    /// </summary>
    public async Task<string> BuildConnectUrlAsync(string userId, string returnPath, string redirectUri, CancellationToken cancellationToken)
    {
        var state = _stateProtector.Protect($"{userId}\n{returnPath}", StateLifetime);
        return await _oauth.BuildAuthorizationUrlAsync(redirectUri, state, cancellationToken);
    }

    /// <summary>
    /// Best-effort extraction of the signed return path (also works on
    /// consent-denied callbacks, where Google still echoes our state).
    /// Returns null when the state is missing or tampered.
    /// </summary>
    public string? PeekReturnPath(string? state)
        => string.IsNullOrEmpty(state) ? null : ParseState(state)?.ReturnPath;

    public async Task CompleteConnectionAsync(string userId, string code, string state, string redirectUri, CancellationToken cancellationToken)
    {
        var parsed = ParseState(state)
            ?? throw new InvalidOperationException("The Google sign-in link was invalid or expired. Please try again.");

        if (!string.Equals(parsed.UserId, userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Google sign-in did not match the signed-in user.");
        }

        var token = await _oauth.ExchangeCodeAsync(code, redirectUri, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            // Without a refresh token we can't sync later; force a re-consent.
            throw new InvalidOperationException("Google did not grant offline access. Please try connecting again.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var link = await db.CalendarIntegrations
            .FirstOrDefaultAsync(c => c.LuminaUserId == userId && c.Provider == Provider, cancellationToken);

        if (link is null)
        {
            link = new CalendarIntegration { LuminaUserId = userId, Provider = Provider };
            await db.CalendarIntegrations.AddAsync(link, cancellationToken);
        }

        link.EncryptedRefreshToken = _tokenProtector.Protect(token.RefreshToken!);
        link.AccountEmail = token.Email;
        link.ConnectedAt = _utcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CalendarConnectionStatus> GetStatusAsync(string userId, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var link = await db.CalendarIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LuminaUserId == userId && c.Provider == Provider, cancellationToken);

        return link is null
            ? new CalendarConnectionStatus(false, null, null)
            : new CalendarConnectionStatus(true, link.AccountEmail, link.LastSyncedAt);
    }

    public async Task DisconnectAsync(string userId, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var link = await db.CalendarIntegrations
            .FirstOrDefaultAsync(c => c.LuminaUserId == userId && c.Provider == Provider, cancellationToken);
        if (link is null)
        {
            return;
        }

        try
        {
            var refreshToken = _tokenProtector.Unprotect(link.EncryptedRefreshToken);
            await _oauth.RevokeAsync(refreshToken, cancellationToken);
        }
        catch
        {
            // Best-effort: revoke failures shouldn't block local disconnect.
        }

        db.CalendarIntegrations.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }

    private (string UserId, string ReturnPath)? ParseState(string state)
    {
        try
        {
            var payload = _stateProtector.Unprotect(state);
            var parts = payload.Split('\n', 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : null;
        }
        catch
        {
            return null;
        }
    }
}
