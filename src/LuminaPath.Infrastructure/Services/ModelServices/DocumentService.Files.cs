using LuminaPath.Core.Entities;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class DocumentService
{
    private async Task<Result<MediaDocument, FailedResult>> UploadDocumentStreamAsync(
        Stream stream,
        string displayName,
        string? contentType,
        IMedia<MediaDocument>? media)
    {
        if (media is not null)
        {
            await DeleteDocument(media.Image);
        }

        var storageName = CreateStorageFileName(displayName, contentType);
        var result = await _storage.UploadAsync(stream, storageName, contentType);
        if (result.Error)
        {
            _logger.LogError("Could not upload file, error: {Status}", result.Status);
            await AuditDocumentFileUploadAsync(
                displayName,
                storageName,
                contentType,
                AuditOutcomes.Failure,
                result.Status);
            return new FailedResult("Could not upload file");
        }

        var document = new MediaDocument
        {
            Name = displayName,
            StorageName = result.Blob.Name,
            Description = string.Empty,
            Path = string.Empty,
            ContentType = result.Blob.ContentType,
            DocumentType = InferDocumentType(displayName, result.Blob.ContentType)
        };
        await AuditDocumentFileUploadAsync(
            document.Name,
            document.StorageName,
            document.ContentType,
            AuditOutcomes.Success,
            documentType: document.DocumentType);
        return document;
    }

    private async Task DeleteDocument(MediaDocument document, LuminaPathDbContext context)
    {
        var existingDocument = await context.MediaDocuments.SingleOrDefaultAsync(d => d.Id == document.Id);
        if (existingDocument is null)
        {
            _logger.LogError("Could not find file, document: {0}", document.Name);
            await AuditDocumentAsync(
                AuditActions.DocumentDeleted,
                document,
                AuditOutcomes.Failure,
                "Document not found.");
            return;
        }

        var storageDeleteStatus = await DeleteStoredFileIfUnreferenced(context, existingDocument);

        if (existingDocument.MediaId != null)
        {
            var media = await context.Set<Media>()
                .FirstOrDefaultAsync(item => item.Id == existingDocument.MediaId);
            if (media != null)
            {
                media.Image = null;
                context.Update(media);
            }
        }

        context.Documents.Remove(existingDocument);
        await context.SaveChangesAsync();
        await AuditDocumentAsync(
            AuditActions.DocumentDeleted,
            existingDocument,
            metadata: new
            {
                source = "DocumentService",
                storageDeleteStatus
            });
    }

    private async Task<string> DeleteStoredFileIfUnreferenced(LuminaPathDbContext context, MediaDocument document)
    {
        var storageName = GetStorageName(document);
        if (string.IsNullOrWhiteSpace(storageName))
        {
            return "NoStorageName";
        }

        var isReferenced = await context.Documents
            .AsNoTracking()
            .AnyAsync(item => item.Id != document.Id
                && (item.StorageName == storageName || item.StorageName == null && item.Name == storageName));

        if (isReferenced)
        {
            return "StillReferenced";
        }

        var result = await _storage.DeleteAsync(storageName);
        if (result.Error)
        {
            _logger.LogWarning("Could not delete stored file {FileName}, removing database document anyway. Error: {Status}", storageName, result.Status);
            return "StorageDeleteFailed";
        }

        return "Deleted";
    }

    private static string GetStorageName(MediaDocument document)
    {
        return document.StorageName ?? document.Name ?? string.Empty;
    }

    private static string GetDisplayFileName(string fileName)
    {
        var displayName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(displayName)
            ? "upload"
            : displayName.Trim();
    }

    private static string CreateStorageFileName(string fileName, string? contentType)
    {
        var displayName = GetDisplayFileName(fileName);
        var extension = Path.GetExtension(displayName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GetExtensionFromContentType(contentType);
        }

        return $"{Guid.NewGuid():N}{extension}";
    }

    private static void ValidateDocument(MediaDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Name)
            && string.IsNullOrWhiteSpace(document.StorageName)
            && string.IsNullOrWhiteSpace(document.Path))
        {
            throw new InvalidOperationException("Document needs a file name or path.");
        }
    }

    private static string GetExtensionFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/apng" => ".apng",
            "image/avif" => ".avif",
            "image/bmp" => ".bmp",
            "image/gif" => ".gif",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/svg+xml" => ".svg",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            _ => string.Empty
        };
    }

    private static DocumentType InferDocumentType(string fileName, string? contentType)
    {
        if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentType.Image;
        }

        if (contentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentType.PDF;
        }

        var extension = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        return extension switch
        {
            "PNG" or "JPG" or "JPEG" or "WEBP" or "GIF" or "BMP" or "SVG" => DocumentType.Image,
            "PDF" => DocumentType.PDF,
            "XLS" or "XLSX" or "CSV" => DocumentType.Excel,
            "DOC" or "DOCX" or "ODT" or "TXT" or "MD" => DocumentType.Document,
            _ => DocumentType.Others
        };
    }
}
