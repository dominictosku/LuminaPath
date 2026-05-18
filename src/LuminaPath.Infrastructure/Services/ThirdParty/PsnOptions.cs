using LuminaPath.Infrastructure.Configuration;

namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class PsnOptions
{
    public const string SectionName = "PSN";

    public string AuthorizationBaseUrl { get; set; } = "https://ca.account.sony.com/api/authz/v3";
    public string ProfileBaseUrl { get; set; } = "https://us-prof.np.community.playstation.net";
    public string ApiBaseUrl { get; set; } = "https://m.np.playstation.com/api";
    public string ClientId { get; set; } = "09515159-7237-4370-9b40-3806e67c0891";
    public string RedirectUri { get; set; } = "com.scee.psxandroid.scecompcall://redirect";
    public string Scope { get; set; } = "psn:mobile.v2.core psn:clientapp";
    public string TokenAuthorizationHeader { get; set; } = "Basic MDk1MTUxNTktNzIzNy00MzcwLTliNDAtMzgwNmU2N2MwODkxOnVjUGprYTV0bnRCMktxc1A=";

    internal static bool HasValidUrls(PsnOptions options)
    {
        return InfrastructureOptionValidation.IsHttpUrl(options.AuthorizationBaseUrl)
            && InfrastructureOptionValidation.IsHttpUrl(options.ProfileBaseUrl)
            && InfrastructureOptionValidation.IsHttpUrl(options.ApiBaseUrl);
    }

    internal static bool HasRequiredOAuthSettings(PsnOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.ClientId)
            && !string.IsNullOrWhiteSpace(options.RedirectUri)
            && !string.IsNullOrWhiteSpace(options.Scope)
            && !string.IsNullOrWhiteSpace(options.TokenAuthorizationHeader)
            && options.TokenAuthorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase);
    }
}
