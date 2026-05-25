using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices;

public partial class LuminaUserService
{
    public async Task<UserGridPageDto> GetPaginatedUsers(
        string? searchString = "",
        string? role = null,
        bool? active = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query =
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join identityRole in context.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id into identityRoles
            from identityRole in identityRoles.DefaultIfEmpty()
            select new UserGridItemDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = identityRole == null ? string.Empty : identityRole.Name ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                IsLockedOut = !user.IsActive,
                LockoutEnd = user.LockoutEnd
            };

        var normalizedSearch = searchString?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.UserName.ToLower().Contains(normalizedSearch) ||
                user.FullName.ToLower().Contains(normalizedSearch) ||
                user.Email.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(user => user.Role == role);
        }

        var activeCount = await query.CountAsync(user => !user.IsLockedOut, cancellationToken);
        var lockedCount = await query.CountAsync(user => user.IsLockedOut, cancellationToken);

        if (active.HasValue)
        {
            query = query.Where(user => user.IsLockedOut != active.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(user => user.Role == Roles[0])
            .ThenBy(user => user.FullName == string.Empty ? user.UserName : user.FullName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new UserGridPageDto
        {
            Items = items,
            Total = total,
            Active = activeCount,
            Locked = lockedCount
        };
    }

    public async Task<LuminaUser?> GetUser(string id)
    {
        using var context = await GetDbContextAsync();
        return await context.Users.Include(u => u.LuminaUserInfo).FirstOrDefaultAsync(x => x.Id == id);
    }
}
