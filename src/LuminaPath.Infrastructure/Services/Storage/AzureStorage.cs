using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.Storage
{
    public class AzureStorage : IStorageService
    {
        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;
        private readonly ILogger<AzureStorage> _logger;

        public AzureStorage(string connectionString, string containerName, ILogger<AzureStorage> logger)
        {
            _storageConnectionString = connectionString;
            _storageContainerName = containerName;
            _logger = logger;
        }

        public async Task<List<BlobDto>> ListAsync()
        {
            var container = CreateContainerClient();
            List<BlobDto> files = [];

            await foreach (BlobItem file in container.GetBlobsAsync())
            {
                var uri = container.Uri.ToString();
                var name = file.Name;
                var fullUri = $"{uri}/{name}";

                files.Add(new BlobDto
                {
                    Uri = fullUri,
                    Name = name,
                    ContentType = file.Properties.ContentType
                });
            }

            return files;
        }

        public async Task<BlobDto?> DownloadAsync(string blobFilename)
        {
            var container = CreateContainerClient();
            var blobName = StorageFileNames.GetSafeName(blobFilename);

            try
            {
                var file = container.GetBlobClient(blobName);

                if (await file.ExistsAsync())
                {
                    var data = await file.OpenReadAsync();
                    Stream blobContent = data;
                    var content = await file.DownloadContentAsync();

                    return new BlobDto
                    {
                        Content = blobContent,
                        Name = blobName,
                        ContentType = content.Value.Details.ContentType
                    };
                }
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                _logger.LogError("File {FileName} was not found.", blobName);
            }

            return null;
        }


        public async Task<BlobResponseDto> UploadAsync(Stream blob, string fileName, string? contentType = null)
        {
            BlobResponseDto response = new();
            var container = CreateContainerClient();
            await container.CreateIfNotExistsAsync();
            var blobName = StorageFileNames.WithExtensionFromContentType(fileName, contentType);

            try
            {
                var client = container.GetBlobClient(blobName);

                await using (Stream? data = blob)
                {
                    await client.UploadAsync(data, new BlobHttpHeaders { ContentType = contentType });
                }

                response.Status = $"File {blobName} Uploaded Successfully";
                response.Error = false;
                response.Blob.Uri = client.Uri.AbsoluteUri;
                response.Blob.Name = client.Name;
                response.Blob.ContentType = contentType;

            }
            catch (RequestFailedException ex)
               when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
            {
                _logger.LogError(
                    "File with name {FileName} already exists in container {ContainerName}.",
                    blobName,
                    _storageContainerName);
                response.Status = $"File with name {blobName} already exists. Please use another name to store your file.";
                response.Error = true;
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Could not upload file {FileName}.", blobName);
                response.Status = "Unexpected storage error while uploading the file. Check the server logs.";
                response.Error = true;
                return response;
            }

            return response;
        }

        public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
        {
            var container = CreateContainerClient();
            var blobName = StorageFileNames.GetSafeName(blobFilename);
            var file = container.GetBlobClient(blobName);

            try
            {
                var deleteResult = await file.DeleteIfExistsAsync();
                if (!deleteResult.Value)
                {
                    _logger.LogError("File {FileName} was not found.", blobName);
                    return new BlobResponseDto { Error = true, Status = $"File with name {blobName} not found." };
                }
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                _logger.LogError("File {FileName} was not found.", blobName);
                return new BlobResponseDto { Error = true, Status = $"File with name {blobName} not found." };
            }

            return new BlobResponseDto { Error = false, Status = $"File: {blobName} has been successfully deleted." };

        }

        public async Task<bool> RenameAsync(string oldName, string newName)
        {
            var container = CreateContainerClient();
            var oldBlobName = StorageFileNames.GetSafeName(oldName);
            var newBlobName = StorageFileNames.ForRename(oldBlobName, newName);
            var source = container.GetBlobClient(oldBlobName);
            var target = container.GetBlobClient(newBlobName);

            try
            {
                using var oldFile = await source.OpenReadAsync();
                await target.UploadAsync(oldFile);
                await source.DeleteAsync();
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                _logger.LogError("File {FileName} was not found.", oldBlobName);
                return false;
            }

            return true;
        }

        private BlobContainerClient CreateContainerClient()
        {
            return new BlobContainerClient(_storageConnectionString, _storageContainerName);
        }
    }
}
