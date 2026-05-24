using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Subclasses <see cref="UserManager{TUser}"/> so we can apply the
/// admin-approval gate during self-registration without forking the
/// .NET Identity API endpoints.
///
/// <para>
/// The rule: if <see cref="AuthRegistrationOptions.RequireAdminApproval"/>
/// is true AND the caller is an anonymous HTTP request, force the new
/// user's <c>IsActive</c> to false. Anonymous = no authenticated
/// principal on the current <see cref="HttpContext"/>, which is exactly
/// when self-registration happens (<c>/api/register</c> or the Blazor
/// /Account/Register page). Admin-created users (via
/// <c>LuminaUserService.CreateUser</c>) reach <c>CreateAsync</c> with an
/// authenticated admin principal, so the gate doesn't apply and the
/// admin's chosen <c>Active</c> value sticks. The DataSeeder bootstrap
/// path has no HttpContext at all, so the gate also doesn't apply
/// there — important so the seeded admin can still log in after a
/// fresh deploy.
/// </para>
/// </summary>
public sealed class LuminaUserManager : UserManager<LuminaUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AuthRegistrationOptions> _registrationOptions;

    public LuminaUserManager(
        IUserStore<LuminaUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<LuminaUser> passwordHasher,
        IEnumerable<IUserValidator<LuminaUser>> userValidators,
        IEnumerable<IPasswordValidator<LuminaUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<LuminaUser>> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AuthRegistrationOptions> registrationOptions)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators,
               keyNormalizer, errors, services, logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _registrationOptions = registrationOptions;
    }

    public override Task<IdentityResult> CreateAsync(LuminaUser user, string password)
    {
        ApplyAdminApprovalGate(user);
        return base.CreateAsync(user, password);
    }

    public override Task<IdentityResult> CreateAsync(LuminaUser user)
    {
        ApplyAdminApprovalGate(user);
        return base.CreateAsync(user);
    }

    private void ApplyAdminApprovalGate(LuminaUser user)
    {
        if (!_registrationOptions.CurrentValue.RequireAdminApproval)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // No web request in flight — this is the DataSeeder bootstrap
            // or another code-driven path. Don't second-guess it.
            return;
        }

        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            // An authenticated caller (admin form, integration test that
            // signed in first, etc.) is explicitly creating this user.
            // Whatever they passed for IsActive stays.
            return;
        }

        // Anonymous web caller + approval-required config = lock the new
        // user until an admin flips the flag from the Users page.
        user.IsActive = false;
    }
}
