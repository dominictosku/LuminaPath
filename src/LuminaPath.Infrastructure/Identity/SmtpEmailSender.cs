using System.Net;
using System.Net.Mail;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Sends ASP.NET Identity emails (password reset, email confirmation) over
/// SMTP using the built-in <see cref="SmtpClient"/>. Registered only when
/// <see cref="EmailOptions.IsConfigured"/>; otherwise the app falls back to
/// the no-op sender that shows links on-screen in development.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender<LuminaUser>
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(LuminaUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(LuminaUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(LuminaUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your password",
            $"Please reset your password using the following code: {resetCode}");

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_options.Smtp.User))
        {
            client.Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password);
        }

        try
        {
            await client.SendMailAsync(message);
            // Recipient address is PII, so log only the email kind.
            _logger.LogInformation("Sent Identity email '{Subject}'.", subject);
        }
        catch (Exception ex)
        {
            // Swallow but log: the password-reset flow must look identical
            // whether or not the address exists, otherwise a delivery failure
            // becomes a user-enumeration oracle (existing accounts error,
            // unknown ones silently "succeed"). Operators see this in logs.
            _logger.LogError(ex, "Failed to send Identity email '{Subject}'.", subject);
        }
    }
}
