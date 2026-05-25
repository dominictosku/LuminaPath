using LuminaPath.Core.Enums;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure.Services.Auditing;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class DocumentService
{
    private Task AuditDocumentFileUploadAsync(
        string? displayName,
        string? storageName = null,
        string? contentType = null,
        string outcome = AuditOutcomes.Success,
        string? errorMessage = null,
        DocumentType? documentType = null)
    {
        var resolvedDocumentType = documentType ?? InferDocumentType(displayName ?? string.Empty, contentType);
        return AuditDocumentAsync(
            AuditActions.DocumentUploaded,
            new MediaDocument
            {
                Name = displayName,
                StorageName = storageName,
                ContentType = contentType,
                DocumentType = resolvedDocumentType
            },
            outcome,
            errorMessage,
            metadata: new
            {
                source = "DocumentService",
                contentType,
                documentType = resolvedDocumentType.ToString(),
                storageName
            });
    }

    private async Task AuditDocumentAsync(
        string action,
        MediaDocument document,
        string outcome = AuditOutcomes.Success,
        string? errorMessage = null,
        object? changes = null,
        object? metadata = null)
    {
        if (_auditLog is null)
        {
            return;
        }

        await _auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Document,
            Action = action,
            Outcome = outcome,
            TargetType = nameof(MediaDocument),
            TargetId = document.Id > 0 ? document.Id.ToString() : document.StorageName ?? document.Name,
            TargetName = document.Name ?? document.StorageName ?? document.Path,
            Changes = changes,
            Metadata = metadata ?? new
            {
                source = "DocumentService",
                documentType = document.DocumentType.ToString(),
                contentType = document.ContentType,
                storageName = document.StorageName,
                mediaId = document.MediaId
            },
            ErrorMessage = errorMessage
        });
    }
}
