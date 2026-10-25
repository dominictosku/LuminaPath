namespace LuminaPath.Infrastructure.Services.Application;

/// <summary>A remote copy of a completed local database dump, separate from user uploads.</summary>
public interface IBackupArchive
{
    Task SaveAsync(string filePath, CancellationToken cancellationToken = default);
}
