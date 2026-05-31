using Microsoft.AspNetCore.DataProtection;

namespace LuminaPath.Infrastructure.Services.ThirdParty.GoogleCalendar;

/// <summary>
/// Encrypts/decrypts OAuth refresh tokens before they touch the database,
/// using ASP.NET Data Protection. Refresh tokens are long-lived credentials,
/// so they are never persisted in plaintext.
/// </summary>
public interface ICalendarTokenProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

public sealed class CalendarTokenProtector : ICalendarTokenProtector
{
    private readonly IDataProtector _protector;

    public CalendarTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("GoogleCalendar.RefreshToken");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
