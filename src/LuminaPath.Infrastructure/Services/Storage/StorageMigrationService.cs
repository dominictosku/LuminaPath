using System.Security.Cryptography;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Offline, resumable copying between any two storage providers. Never deletes source files.</summary>
public sealed class StorageMigrationService(ILogger<StorageMigrationService> logger)
{
    public async Task<StorageMigrationResult> MigrateAsync(IStorageService source, IStorageService destination,
        bool dryRun = true, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (ReferenceEquals(source, destination))
            throw new InvalidOperationException("Select different source and destination storage providers.");
        // Preflight names before writing anything: adapters must not silently flatten keys or pick duplicates.
        var sourceFiles = ValidateListing(await source.ListAsync());
        var destinationFiles = ValidateListing(await destination.ListAsync());
        var copied = 0;
        var skipped = 0;
        foreach (var (name, listed) in sourceFiles.OrderBy(file => file.Key, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var original = await source.DownloadAsync(name);
            await using var content = original?.Content
                ?? throw new IOException($"Migration stopped: source file '{name}' could not be read.");
            // Providers may return non-seekable streams; spool one file to bounded-memory local storage.
            await using var snapshot = CreateTemporaryStream();
            await content.CopyToAsync(snapshot, ct);
            snapshot.Position = 0;
            var sourceHash = await SHA256.HashDataAsync(snapshot, ct);
            var contentType = original.ContentType ?? listed.ContentType ?? "application/octet-stream";
            var existing = await destination.DownloadAsync(name);
            if (existing is not null)
            {
                await VerifyAsync(existing, name, contentType, sourceHash, ct);
                skipped++;
                logger.LogInformation("Storage migration skipped identical file {FileName}.", name);
                continue;
            }
            if (destinationFiles.ContainsKey(name))
                throw new IOException($"Migration stopped: destination listed '{name}' but could not read it.");
            if (!dryRun)
            {
                snapshot.Position = 0;
                var response = await destination.UploadExactAsync(snapshot, name, contentType, ct);
                if (response.Error || response.Blob.Name != name)
                    throw new IOException($"Migration stopped while copying '{name}'. Completed copies and source files were retained.");
                var uploaded = await destination.DownloadAsync(name);
                if (uploaded is null) throw new IOException($"Could not verify migrated file '{name}'.");
                await VerifyAsync(uploaded, name, contentType, sourceHash, ct);
            }
            copied++;
            logger.LogInformation("Storage migration {Action} {FileName}.", dryRun ? "would copy" : "verified", name);
        }
        logger.LogInformation("Storage migration: {Copied} files {Action}; {Skipped} identical files skipped. Source files retained.",
            copied, dryRun ? "would be copied" : "copied", skipped);
        return new(copied, skipped, dryRun);
    }

    private static Dictionary<string, BlobDto> ValidateListing(List<BlobDto> files)
    {
        var result = new Dictionary<string, BlobDto>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.Name)) throw new IOException("Storage returned a file without a name.");
            var name = StorageFileNames.GetExactName(file.Name);
            if (!result.TryAdd(name, file)) throw new IOException($"Storage contains duplicate filename '{name}'.");
        }
        return result;
    }

    private static async Task VerifyAsync(BlobDto file, string name, string contentType, byte[] expectedHash, CancellationToken ct)
    {
        if (file.Content is not { } content) throw new IOException($"Cannot verify file '{name}'.");
        await using (content)
        {
            var actualHash = await SHA256.HashDataAsync(content, ct);
            if (!expectedHash.SequenceEqual(actualHash)
                || !string.Equals(contentType, file.ContentType ?? "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Migration stopped: file '{name}' differs in content or content type. Source files were retained; no existing files were overwritten.");
        }
    }

    private static FileStream CreateTemporaryStream()
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None,
            BufferSize = 81920, Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), options);
    }
}

public sealed record StorageMigrationResult(int Copied, int Skipped, bool DryRun);
