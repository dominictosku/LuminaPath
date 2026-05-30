using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

/// <summary>
/// SMTP email delivery for ASP.NET Identity flows (password reset and email
/// confirmation). Optional by design: when <see cref="SmtpOptions.Host"/> is
/// blank the app keeps the built-in no-op sender (which surfaces confirmation
/// links on-screen in development). Set an SMTP host plus a from-address to
/// turn on real self-service password reset.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Envelope/From address shown to recipients, e.g. "noreply@example.com".</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Display name shown next to the From address.</summary>
    public string FromName { get; set; } = "LuminaPath";

    /// <summary>True once both an SMTP host and a from-address are configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Smtp.Host) && !string.IsNullOrWhiteSpace(FromAddress);

    // Validation only bites once an SMTP host is set, so the default
    // (no email) configuration stays valid and the app boots without SMTP.
    public static bool HasValidFromAddressWhenConfigured(EmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.FromAddress)
            && MailAddress.TryCreate(options.FromAddress, out _);
    }

    public static bool HasValidPortWhenConfigured(EmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            return true;
        }

        return options.Smtp.Port is > 0 and <= 65535;
    }

    public static EmailOptions FromConfiguration(IConfiguration config)
        => config.GetSection(SectionName).Get<EmailOptions>() ?? new EmailOptions();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? User { get; set; }

    public string? Password { get; set; }

    /// <summary>
    /// Use STARTTLS (the common port-587 setup). Maps to
    /// <see cref="System.Net.Mail.SmtpClient.EnableSsl"/>.
    /// </summary>
    public bool UseStartTls { get; set; } = true;
}
