using LuminaPath.Infrastructure.Hubs;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Application;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LuminaPath.Infrastructure;

public static class ServerEndpointRouteBuilderExtensions
{
    public static void ConfigureServer(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<DirectMessageHub>("/hubs/messages");
        app.MapGroup("/api")
            .MapIdentityApi<LuminaUser>();
        app.MapPost("/api/logout", async (SignInManager<LuminaUser> signInManager, [FromBody] object empty) =>
            {
                if (empty != null)
                {
                    await signInManager.SignOutAsync();
                    return Results.Ok();
                }

                return Results.Unauthorized();
            })
            .RequireAuthorization();

        app.MapGet("/api/admin/database-backups/{fileName}/download", DownloadDatabaseBackupAsync)
            .RequireAuthorization(policy => policy.RequireRole("Administrator"));

        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "LuminaPath" }))
            .AllowAnonymous();
    }

    private static async Task<IResult> DownloadDatabaseBackupAsync(
        HttpContext context,
        string fileName,
        DatabaseBackupService backupService,
        AuditLogService auditLog,
        CancellationToken cancellationToken)
    {
        var actor = AuditLogService.ActorFromPrincipal(context.User);
        var backup = await backupService.GetBackupAsync(fileName, cancellationToken);
        if (backup is null)
        {
            await auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.DatabaseBackupDownloaded,
                Outcome = AuditOutcomes.Failure,
                Actor = actor,
                TargetType = "DatabaseBackup",
                TargetId = fileName,
                TargetName = fileName,
                ErrorMessage = "Database backup was not found."
            }, cancellationToken);

            return Results.NotFound();
        }

        await auditLog.RecordAsync(new AuditLogEntry
        {
            Category = AuditCategories.Admin,
            Action = AuditActions.DatabaseBackupDownloaded,
            Outcome = AuditOutcomes.Success,
            Actor = actor,
            TargetType = "DatabaseBackup",
            TargetId = backup.FileName,
            TargetName = backup.FileName,
            Metadata = new
            {
                sizeBytes = backup.SizeBytes,
                createdAt = backup.CreatedAt
            }
        }, cancellationToken);

        return Results.File(
            backup.FullPath,
            contentType: "application/octet-stream",
            fileDownloadName: backup.FileName);
    }
}
