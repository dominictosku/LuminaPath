namespace LuminaPath.Infrastructure.Services.ThirdParty;

public sealed class SteamOptions
{
    public const string SectionName = "Steam";

    public string ApiBaseUrl { get; set; } = "https://api.steampowered.com";

    public string StoreBaseUrl { get; set; } = "https://store.steampowered.com";

    internal static bool HasValidUrls(SteamOptions options)
    {
        return LuminaPath.Infrastructure.Configuration.InfrastructureOptionValidation.IsHttpUrl(options.ApiBaseUrl)
            && LuminaPath.Infrastructure.Configuration.InfrastructureOptionValidation.IsHttpUrl(options.StoreBaseUrl);
    }
}
