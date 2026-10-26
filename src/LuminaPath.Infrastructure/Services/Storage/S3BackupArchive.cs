using LuminaPath.Infrastructure.Services.Application;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class S3BackupArchive(S3Storage storage) : IBackupArchive
{
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var file = File.OpenRead(path);
        var result = await storage.UploadAsync("backups", file, Path.GetFileName(path), "application/octet-stream", cancellationToken);
        if (result.Error) throw new IOException("S3 backup upload failed. The completed local dump was retained.");
    }
}
