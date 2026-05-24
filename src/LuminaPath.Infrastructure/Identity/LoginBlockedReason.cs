namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Shared keys for surfacing *why* a sign-in attempt was rejected.
///
/// <para>
/// <see cref="SignInManager{TUser}.PasswordSignInAsync"/> collapses every
/// pre-sign-in failure (email not confirmed, account inactive, etc.)
/// into a single <see cref="SignInResult.IsNotAllowed"/> flag — useful
/// for security (no enumeration), but unhelpful when we want to tell a
/// freshly-registered user "you're waiting on admin approval" instead
/// of the generic "invalid credentials".
/// </para>
///
/// <para>
/// <see cref="LuminaSignInManager"/> writes the reason in two places when
/// it blocks a sign-in: <see cref="HttpContext.Items"/> (so server-side
/// Blazor pages can read it inline) and a response header (so the
/// Angular SPA can read it off the 401 from <c>/api/login</c>). The
/// header is allowlisted by CORS via <c>AddCors</c> so cross-origin
/// fetches can actually see it.
/// </para>
/// </summary>
public static class LoginBlockedReason
{
    /// <summary>
    /// Reason value written when a sign-in is rejected because the
    /// user's <c>IsActive</c> flag is false. Stable string — both the
    /// Blazor login page and the Angular SPA pattern-match on it.
    /// </summary>
    public const string InactiveAccount = "InactiveAccount";

    /// <summary>
    /// Key for <see cref="HttpContext.Items"/>. Used by the Blazor
    /// Login.razor to discover the reason after PasswordSignInAsync
    /// returns IsNotAllowed.
    /// </summary>
    public const string HttpContextItemKey = "LoginBlockedReason";

    /// <summary>
    /// Response header name added to the 401 from <c>/api/login</c> so
    /// the Angular SPA can surface a tailored message. Custom header
    /// names (the <c>X-</c> prefix is conventional, not required) are
    /// hidden from cross-origin XHR by default — see <c>AddCors</c>
    /// where this is added to <c>WithExposedHeaders</c>.
    /// </summary>
    public const string ResponseHeaderName = "X-Login-Blocked-Reason";
}
