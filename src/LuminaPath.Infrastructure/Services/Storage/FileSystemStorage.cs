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
                    Uri = filePath,
                    Name = Path.GetFileName(filePath),
                    ContentType = StorageContentTypes.GetContentType(filePath)
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
                Content = File.OpenRead(filePath),
                Name = Path.GetFileName(filePath),
                ContentType = StorageContentTypes.GetContentType(filePath)
            };

            return Task.FromResult<BlobDto?>(blob);
        }

        public async Task<BlobResponseDto> UploadAsync(Stream blob, string fileName, string? contentType = null)
        {
            BlobResponseDto response = new();
            var filePath = GetStorageFilePath(StorageFileNames.WithExtensionFromContentType(fileName, contentType));
            var storedFileName = Path.GetFileName(filePath);

            if (File.Exists(filePath))
            {
                response.Status = $"File with name {storedFileName} already exists. Please use another name to store your file.";
                response.Error = true;
                return response;
            }

            try
            {
                await using var fileStream = File.Create(filePath);
                await blob.CopyToAsync(fileStream);

                response.Status = $"File {storedFileName} Uploaded Successfully";
                response.Error = false;
                response.Blob.Uri = filePath;
                response.Blob.Name = storedFileName;
                response.Blob.ContentType = StorageContentTypes.GetContentType(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not upload file {FileName}.", storedFileName);
                response.Status = "Unexpected storage error while uploading the file. Check the server logs.";
                response.Error = true;
            }

            return response;
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

            File.Move(oldPath, newPath);
            DeleteContentType(oldPath);
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
