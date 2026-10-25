using System.Security.Cryptography;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Copy existing filesystem uploads without changing database storage names.</summary>
public sealed class StorageMigrationService(IStorageService destination, ILogger<StorageMigrationService> logger)
{
    public async Task<StorageMigrationResult> MigrateAsync(string sourceDirectory, bool dryRun, CancellationToken ct = default)
    {
        if (destination is not GoogleDriveStorage)
            throw new InvalidOperationException("Storage migration requires Storage:Provider=GoogleDrive.");
        var root = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Source upload directory does not exist.");
        var copied = 0;
        var skipped = 0;
        foreach (var path in Directory.EnumerateFiles(root).Order(StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            if (name.EndsWith(".contenttype", StringComparison.OrdinalIgnoreCase)) continue;
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Migration does not follow symbolic links.");
            await using var source = File.OpenRead(path);
            var existing = await destination.DownloadAsync(name);
            if (existing?.Content is { } content)
            {
                await using (content)
                {
                    var originalHash = await SHA256.HashDataAsync(source, ct);
                    var destinationHash = await SHA256.HashDataAsync(content, ct);
                    if (!originalHash.SequenceEqual(destinationHash))
                        throw new IOException($"Migration stopped: existing file '{name}' has different content. No files were overwritten.");
                }
                skipped++;
                continue;
            }
            if (!dryRun)
            {
                source.Position = 0;
                var response = await destination.UploadAsync(source, name, StorageContentTypes.GetContentType(name));
                if (response.Error) throw new IOException($"Migration stopped while copying '{name}'. Completed copies and source files were retained.");
                var uploaded = await destination.DownloadAsync(name);
                if (uploaded?.Content is not { } verification) throw new IOException("Could not verify migrated upload.");
                await using (verification)
                {
                    source.Position = 0;
                    var sourceHash = await SHA256.HashDataAsync(source, ct);
                    var copiedHash = await SHA256.HashDataAsync(verification, ct);
                    if (!sourceHash.SequenceEqual(copiedHash))
                        throw new IOException("Migrated file checksum did not match. Source files were retained.");
                }
            }
            copied++;
        }
        logger.LogInformation("Storage migration: {Copied} files {Action}; {Skipped} identical files skipped. Source files retained.",
            copied, dryRun ? "would be copied" : "copied", skipped);
        return new(copied, skipped, dryRun);
    }
}

public sealed record StorageMigrationResult(int Copied, int Skipped, bool DryRun);
