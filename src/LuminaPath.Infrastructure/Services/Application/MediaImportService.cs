using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ThirdParty;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class MediaImportService
{
    private readonly SteamService _steamService;

    public MediaImportService(SteamService steamService)
    {
        _steamService = steamService;
    }

    public Task<bool> IsSteamConfiguredAsync(CancellationToken cancellationToken)
    {
        return _steamService.IsConfiguredAsync(cancellationToken);
    }

    public Task<SteamLibraryPreview?> PreviewSteamLibraryAsync(string identifier, CancellationToken cancellationToken)
    {
        return _steamService.PreviewLibraryAsync(identifier, cancellationToken);
    }

    public Task<SteamImportResult> ImportSteamLibraryAsync(
        LuminaUser user,
        string identifier,
        CancellationToken cancellationToken)
    {
        return _steamService.ImportLibraryAsync(user, identifier, cancellationToken);
    }
}
