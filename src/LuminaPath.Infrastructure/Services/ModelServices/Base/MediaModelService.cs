using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Mapping;
using LuminaPath.Core.Models.Base;
using LuminaPath.Infrastructure.Services.Auditing;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices.Base;

public abstract partial class MediaModelService<TMedia, TUserMedia> : GenericModelService<TMedia>
    where TMedia : Media
    where TUserMedia : MyMedia, IMyMedia
{
    private readonly DocumentService _documentService;
    private readonly AuditLogService? _auditLog;

    protected MediaModelService(
        IDbContextFactory<LuminaPathDbContext> dbContextFactory,
        DocumentService documentService,
        IObjectMapper mapper,
        AuditLogService? auditLog = null)
        : base(dbContextFactory, mapper)
    {
        _documentService = documentService;
        _auditLog = auditLog;
    }

    protected virtual Func<IQueryable<TMedia>, IOrderedQueryable<TMedia>> DefaultOrderBy
        => media => media
            .OrderByDescending(item => item.ReleaseDate)
            .ThenBy(item => item.Name);

    protected abstract IQueryable<TMedia> IncludeUserLibrary(IQueryable<TMedia> query, string userId);

    protected abstract Expression<Func<TMedia, bool>> IsInUserLibrary(string userId);

    protected abstract string UserLibraryNavigationName { get; }

    public override async Task<Result<TMedia, FailedResult>> PostAsync(TMedia entity)
    {
        var conflict = await EnsureNameUnique(entity);
        if (conflict is not null)
        {
            await AuditMediaAsync(AuditActions.MediaCreated, AuditOutcomes.Failure, entity, errorMessage: FailureMessage(conflict));
            return conflict;
        }

        var result = await base.PostAsync(entity);
        await result.Match(
            async media =>
            {
                await AuditMediaAsync(AuditActions.MediaCreated, AuditOutcomes.Success, media);
                return true;
            },
            async failure =>
            {
                await AuditMediaAsync(AuditActions.MediaCreated, AuditOutcomes.Failure, entity, errorMessage: FailureMessage(failure));
                return false;
            });
        return result;
    }

    public override async Task<Result<TMedia, FailedResult>> PutAsync(TMedia entity)
    {
        var conflict = await EnsureNameUnique(entity);
        if (conflict is not null)
        {
            await AuditMediaAsync(AuditActions.MediaUpdated, AuditOutcomes.Failure, entity, errorMessage: FailureMessage(conflict));
            return conflict;
        }

        var existing = await GetMediaSnapshotAsync(entity.Id);
        var result = await base.PutAsync(entity);
        await result.Match(
            async media =>
            {
                await AuditMediaAsync(
                    AuditActions.MediaUpdated,
                    AuditOutcomes.Success,
                    media,
                    changes: existing is null ? null : MediaChanges(existing, media));
                return true;
            },
            async failure =>
            {
                await AuditMediaAsync(AuditActions.MediaUpdated, AuditOutcomes.Failure, entity, errorMessage: FailureMessage(failure));
                return false;
            });
        return result;
    }

    private async Task<ValidationFailed?> EnsureNameUnique(TMedia entity)
    {
        await using var context = await GetDbContextAsync();
        var exists = await context.Set<TMedia>()
            .AsNoTracking()
            .AnyAsync(media => media.Id != entity.Id && media.Name == entity.Name);

        return exists
            ? new ValidationFailed(new ValidationFailure(nameof(Media.Name), "Title is already registered"))
            : null;
    }

    public override async Task<Result<int, FailedResult>> DeleteAsync(int? id)
    {
        if (id is null)
        {
            await AuditMediaAsync(
                AuditActions.MediaDeleted,
                AuditOutcomes.Failure,
                targetId: null,
                targetName: null,
                errorMessage: "Entry not found");
            return new FailedResult("Entry not found");
        }

        await using var context = await GetDbContextAsync();
        var existing = await context.Set<TMedia>()
            .Include(media => media.Image)
            .FirstOrDefaultAsync(media => media.Id == id.Value);

        if (existing is null)
        {
            await AuditMediaAsync(
                AuditActions.MediaDeleted,
                AuditOutcomes.Failure,
                targetId: id.Value.ToString(),
                targetName: null,
                errorMessage: "Entry not found");
            return new FailedResult("Entry not found");
        }

        await _documentService.DeleteMediaDocument(existing, context);
        var result = await base.DeleteAsync(id);
        await result.Match(
            async deletedId =>
            {
                await AuditMediaAsync(AuditActions.MediaDeleted, AuditOutcomes.Success, existing);
                return true;
            },
            async failure =>
            {
                await AuditMediaAsync(AuditActions.MediaDeleted, AuditOutcomes.Failure, existing, errorMessage: FailureMessage(failure));
                return false;
            });
        return result;
    }

}
