using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage
{
    public class FileSystemStorage : IStorageService
    {
        private readonly string _storagePath;
        private readonly ILogger<FileSystemStorage> _logger;

        public FileSystemStorage(string storagePath, ILogger<FileSystemStorage> logger)
        {
            _storagePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storagePath));
            _logger = logger;
            Directory.CreateDirectory(_storagePath);
        }

        public Task<List<BlobDto>> ListAsync()
        {
            var files = Directory
                .EnumerateFiles(_storagePath)
                .Where(filePath => !IsMetadataFile(filePath))
                .Select(filePath => new BlobDto
                {
                    Uri = CheckRegularFile(filePath),
                    Name = Path.GetFileName(filePath),
                    ContentType = ReadContentType(filePath)
                })
                .ToList();

            return Task.FromResult(files);
        }

        public Task<BlobDto?> DownloadAsync(string blobFilename)
        {
            var filePath = GetStorageFilePath(blobFilename);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File {FileName} was not found.", blobFilename);
                return Task.FromResult<BlobDto?>(null);
            }

            BlobDto blob = new()
            {
                Content = File.OpenRead(CheckRegularFile(filePath)),
                Name = Path.GetFileName(filePath),
                ContentType = ReadContentType(filePath)
            };

            return Task.FromResult<BlobDto?>(blob);
        }

        public Task<BlobResponseDto> UploadAsync(Stream blob, string fileName, string? contentType = null) =>
            UploadExactAsync(blob, StorageFileNames.WithExtensionFromContentType(fileName, contentType), contentType);

        public async Task<BlobResponseDto> UploadExactAsync(Stream blob, string fileName, string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            var storedFileName = StorageFileNames.GetExactName(fileName);
            if (IsMetadataFile(storedFileName))
                throw new InvalidOperationException("The .contenttype suffix is reserved for filesystem metadata.");
            var filePath = GetStorageFilePath(storedFileName);
            if (File.Exists(filePath)) return new BlobResponseDto { Error = true, Status = "File already exists." };
            var staging = Path.Combine(_storagePath, ".pending");
            Directory.CreateDirectory(staging);
            CheckRegularFile(staging);
            var temporary = Path.Combine(staging, Guid.NewGuid().ToString("N"));
            var created = false;
            contentType ??= StorageContentTypes.GetContentType(fileName) ?? "application/octet-stream";
            try
            {
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    81920, FileOptions.Asynchronous))
                    await blob.CopyToAsync(stream, cancellationToken);
                await File.WriteAllTextAsync(temporary + ".contenttype", contentType, cancellationToken);
                // Publish only a complete file; a concurrent create must never be overwritten.
                File.Move(temporary, filePath, overwrite: false);
                created = true;
                File.Move(temporary + ".contenttype", GetContentTypeMetadataPath(filePath), overwrite: true);
                return new BlobResponseDto
                {
                    Status = "File uploaded successfully.",
                    Blob = new BlobDto { Uri = filePath, Name = storedFileName, ContentType = contentType }
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                if (created) File.Delete(filePath);
                _logger.LogError(ex, "Could not upload file {FileName}.", storedFileName);
                return new BlobResponseDto { Error = true, Status = "File upload failed. Check the server logs." };
            }
            finally
            {
                File.Delete(temporary);
                File.Delete(temporary + ".contenttype");
            }
        }

        public Task<BlobResponseDto> DeleteAsync(string blobFilename)
        {
            var filePath = GetStorageFilePath(blobFilename);
            var storedFileName = Path.GetFileName(filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File {FileName} was not found.", storedFileName);
                return Task.FromResult(new BlobResponseDto
                {
                    Error = true,
                    Status = $"File with name {storedFileName} not found."
                });
            }

            File.Delete(filePath);
            DeleteContentType(filePath);

            return Task.FromResult(new BlobResponseDto
            {
                Error = false,
                Status = $"File: {storedFileName} has been successfully deleted."
            });
        }

        public Task<bool> RenameAsync(string oldName, string newName)
        {
            var oldPath = GetStorageFilePath(oldName);
            var newPath = GetStorageFilePath(StorageFileNames.ForRename(oldPath, newName));

            if (!File.Exists(oldPath))
            {
                _logger.LogError("File {FileName} was not found.", oldName);
                return Task.FromResult(false);
            }

            if (File.Exists(newPath))
            {
                _logger.LogError("File {FileName} already exists.", newName);
                return Task.FromResult(false);
            }

            CheckRegularFile(oldPath);
            File.Move(oldPath, newPath);
            var metadata = GetContentTypeMetadataPath(oldPath);
            if (File.Exists(metadata))
                File.Move(CheckRegularFile(metadata), GetContentTypeMetadataPath(newPath), overwrite: true);
            return Task.FromResult(true);
        }

        private string GetStorageFilePath(string fileName)
        {
            var safeFileName = StorageFileNames.GetSafeName(fileName);

            var fullPath = Path.GetFullPath(Path.Combine(_storagePath, safeFileName));

            if (!fullPath.StartsWith(_storagePath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !fullPath.Equals(_storagePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("File name resolves outside the storage directory.");
            }

            return fullPath;
        }

        private static string CheckRegularFile(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Storage does not follow symbolic links.");
            return path;
        }

        private static string? ReadContentType(string path)
        {
            var metadata = GetContentTypeMetadataPath(path);
            return File.Exists(metadata)
                ? File.ReadAllText(CheckRegularFile(metadata))
                : StorageContentTypes.GetContentType(path);
        }

        private static void DeleteContentType(string filePath)
        {
            var metadataPath = GetContentTypeMetadataPath(filePath);

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }

        private static bool IsMetadataFile(string filePath)
        {
            return filePath.EndsWith(".contenttype", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetContentTypeMetadataPath(string filePath)
        {
            return $"{filePath}.contenttype";
        }
    }
}
