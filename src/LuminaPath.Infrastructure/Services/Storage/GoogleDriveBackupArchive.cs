using LuminaPath.Infrastructure.Services.Application;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class GoogleDriveBackupArchive(GoogleDriveClient drive) : IBackupArchive
{
    public async Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        await drive.UploadAsync("backups", Path.GetFileName(filePath), stream, "application/octet-stream", cancellationToken);
    }
}
