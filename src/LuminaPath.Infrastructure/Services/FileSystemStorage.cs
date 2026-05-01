using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services
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
                    ContentType = GetStoredContentType(filePath)
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
                ContentType = GetStoredContentType(filePath)
            };

            return Task.FromResult<BlobDto?>(blob);
        }

        public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
        {
            await using var stream = blob.OpenReadStream();
            return await UploadAsync(stream, blob.FileName, blob.ContentType);
        }

        public async Task<BlobResponseDto> UploadAsync(Stream blob, string fileName, string? contentType = null)
        {
            BlobResponseDto response = new();
            var filePath = GetStorageFilePath(GetFileNameWithExtension(fileName, contentType));
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
                response.Blob.ContentType = GetContentType(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not upload file {FileName}.", storedFileName);
                response.Status = $"Unexpected error: {ex.Message}";
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
            var newPath = GetStorageFilePath(GetFileNameForRename(oldPath, newName));

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
            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                throw new InvalidOperationException("File name is required.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(_storagePath, safeFileName));

            if (!fullPath.StartsWith(_storagePath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !fullPath.Equals(_storagePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("File name resolves outside the storage directory.");
            }

            return fullPath;
        }

        private static string GetFileNameWithExtension(string fileName, string? contentType)
        {
            var safeFileName = Path.GetFileName(fileName);

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(safeFileName)))
            {
                return safeFileName;
            }

            var extension = GetExtensionFromContentType(contentType);
            return extension is null
                ? safeFileName
                : $"{safeFileName}{extension}";
        }

        private static string GetFileNameForRename(string oldPath, string newName)
        {
            var safeNewName = Path.GetFileName(newName);

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(safeNewName)))
            {
                return safeNewName;
            }

            return $"{safeNewName}{Path.GetExtension(oldPath)}";
        }

        private static string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".apng" => "image/apng",
                ".avif" => "image/avif",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private static string? GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return null;
            }

            var value = contentType.ToLowerInvariant();

            if (value.Contains(".apng") || value.Contains("apng"))
            {
                return ".apng";
            }

            if (value.Contains(".jpeg") || value.Contains("jpeg"))
            {
                return ".jpeg";
            }

            if (value.Contains(".jpg") || value.Contains("jpg"))
            {
                return ".jpg";
            }

            if (value.Contains(".png") || value.Contains("png"))
            {
                return ".png";
            }

            if (value.Contains(".webp") || value.Contains("webp"))
            {
                return ".webp";
            }

            if (value.Contains(".gif") || value.Contains("gif"))
            {
                return ".gif";
            }

            if (value.Contains(".bmp") || value.Contains("bmp"))
            {
                return ".bmp";
            }

            if (value.Contains(".avif") || value.Contains("avif"))
            {
                return ".avif";
            }

            if (value.Contains(".svg") || value.Contains("svg"))
            {
                return ".svg";
            }

            if (value.Contains(".pdf") || value.Contains("pdf"))
            {
                return ".pdf";
            }

            if (value.Contains(".txt") || value.Contains("text/plain"))
            {
                return ".txt";
            }

            return null;
        }

        private static string GetStoredContentType(string filePath)
        {
            return GetContentType(filePath);
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
