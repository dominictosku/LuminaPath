using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Subclasses <see cref="SignInManager{TUser}"/> so we can gate sign-in
/// on our own <c>IsActive</c> flag in addition to Identity's built-in
/// checks (lockout, two-factor, email confirmation).
///
/// <para>
/// We hook <see cref="CanSignInAsync"/> rather than overriding the
/// password endpoint because <c>CanSignInAsync</c> is the single
/// chokepoint every SignInManager path runs through (PasswordSignInAsync,
/// ExternalLoginSignInAsync, RefreshSignInAsync, two-factor flow, etc).
/// Putting the check anywhere else would leave one of those paths open.
/// </para>
///
/// <para>
/// When the flag is false we also write an audit log entry so admins can
/// see "X tried to log in while inactive" — useful when newly-registered
/// users hit the admin-approval gate, or when a previously-active user
/// is later locked.
/// </para>
/// </summary>
public sealed class LuminaSignInManager : SignInManager<LuminaUser>
{
    private readonly AuditLogService _auditLog;

    public LuminaSignInManager(
        UserManager<LuminaUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<LuminaUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<LuminaUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<LuminaUser> confirmation,
        AuditLogService auditLog)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _auditLog = auditLog;
    }

    public override async Task<bool> CanSignInAsync(LuminaUser user)
    {
        // Always defer to the base check first — that handles
        // RequireConfirmedEmail / RequireConfirmedPhoneNumber /
        // RequireConfirmedAccount semantics consistently with the rest
        // of Identity. Only if the base says "ok" do we apply our own
        // gate; that ordering matches the principle of "most specific
        // failure reason wins" for the audit log.
        var baseOk = await base.CanSignInAsync(user);
        if (!baseOk)
        {
            return false;
        }

        if (user.IsActive)
        {
            return true;
        }

        await _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Account,
            Action = AuditActions.Login,
            Outcome = AuditOutcomes.Failure,
            TargetType = "User",
            TargetId = user.Id,
            TargetName = user.Email,
            Metadata = new
            {
                source = "LuminaSignInManager",
                reason = "InactiveAccount"
            },
            ErrorMessage = "Sign-in blocked: account is not active."
        });

        Logger.LogInformation("Sign-in blocked for user '{UserId}' — IsActive is false.", user.Id);
        return false;
    }
}
