using System.Runtime.CompilerServices;
using LuminaPath.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Auditing;

public sealed partial class AuditSaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, PendingAuditEntries> _pendingAudits = new();

    private void AddAuditEntries(DbContext? context)
    {
        if (context is not LuminaPathDbContext dbContext)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();
        var auditLogs = dbContext.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is not AuditLog)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(CreateAuditLog)
            .OfType<AuditLog>()
            .ToList();

        if (auditLogs.Count == 0)
        {
            _pendingAudits.Remove(dbContext);
            return;
        }

        _pendingAudits.Remove(dbContext);
        _pendingAudits.Add(dbContext, new PendingAuditEntries(auditLogs));
    }

    private void SavePendingAuditEntries(DbContext? context)
    {
        if (context is null || !_pendingAudits.TryGetValue(context, out var pending))
        {
            return;
        }

        _pendingAudits.Remove(context);
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
            using var auditContext = dbContextFactory.CreateDbContext();
            auditContext.AuditLogs.AddRange(pending.Entries);
            auditContext.SaveChanges();
        }
        catch
        {
        }
    }

    private async Task SavePendingAuditEntriesAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !_pendingAudits.TryGetValue(context, out var pending))
        {
            return;
        }

        _pendingAudits.Remove(context);
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LuminaPathDbContext>>();
            await using var auditContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            auditContext.AuditLogs.AddRange(pending.Entries);
            await auditContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private void RemovePendingAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        _pendingAudits.Remove(context);
    }

    private sealed class PendingAuditEntries
    {
        public PendingAuditEntries(IReadOnlyList<AuditLog> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<AuditLog> Entries { get; }
    }
}
