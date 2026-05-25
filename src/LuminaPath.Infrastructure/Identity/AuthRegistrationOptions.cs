using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Knobs for the self-registration flow. Bound from the <c>Auth</c>
/// config section so each key can be overridden by an environment
/// variable (e.g. <c>Auth__RequireAdminApproval=true</c>).
/// </summary>
public sealed class AuthRegistrationOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// When true, newly self-registered users land with
    /// <c>IsActive = false</c> and cannot sign in until an admin flips
    /// the flag from the Users page. Defaults to true so deployments
    /// stay closed by default. Development overrides this to false in
    /// appsettings.Development.json.
    /// </summary>
    public bool RequireAdminApproval { get; set; } = true;

    internal static AuthRegistrationOptions FromConfiguration(IConfiguration config)
    {
        var options = new AuthRegistrationOptions();
        config.GetSection(SectionName).Bind(options);
        return options;
    }
}
