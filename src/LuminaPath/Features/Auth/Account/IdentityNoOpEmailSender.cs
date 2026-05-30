using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LuminaPath.Features.Auth.Account
{
    // Fallback sender used when no SMTP is configured (see EmailOptions /
    // SmtpEmailSender). RegisterConfirmation.razor deliberately keys off
    // "EmailSender is IdentityNoOpEmailSender" to show the confirmation link
    // on-screen only while this no-op fallback is active; once SMTP is
    // configured the real sender is used and that branch is skipped.
    internal sealed class IdentityNoOpEmailSender : IEmailSender<LuminaUser>
    {
        private readonly IEmailSender emailSender = new NoOpEmailSender();

        public Task SendConfirmationLinkAsync(LuminaUser user, string email, string confirmationLink) =>
            emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(LuminaUser user, string email, string resetLink) =>
            emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(LuminaUser user, string email, string resetCode) =>
            emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
    }
}
