using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public class LuminaUserService
    {
        public static readonly string[] Roles = ["Administrator", "Editor"];
        private readonly IDbContextFactory<LuminaPathDbContext> _dbContextFactory;
        private readonly UserManager<LuminaUser> _userManager;
        public LuminaUserService(IDbContextFactory<LuminaPathDbContext> dbContextFactory, UserManager<LuminaUser> userManager)
        {
            _dbContextFactory = dbContextFactory;
            _userManager = userManager;
        }

        protected async Task<LuminaPathDbContext> GetDbContextAsync()
        {
            return await _dbContextFactory.CreateDbContextAsync();
        }

        public async Task<UserGridPageDto> GetPaginatedUsers(
            string? searchString = "",
            string? role = null,
            bool? active = null,
            int skip = 0,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

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
                    IsLockedOut = user.LockoutEnabled && user.LockoutEnd != null && user.LockoutEnd >= now,
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

        public async Task<IdentityResult> CreateUser(UserDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Password is required." });
            }

            var lockedOut = !model.Active;
            var applicationUser = new LuminaUser
            {
                FullName = model.UserName,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                LockoutEnabled = lockedOut,
                LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddDays(60) : null,
                EmailConfirmed = true
            };
            var password = model.Password;
            var state = await _userManager.CreateAsync(applicationUser, password!);
            if (state.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user is not null)
                {
                    await SetUserRole(user, model.Role);
                }
            }
            return state;
        }

        public async Task<IdentityResult> UpdateUser(UserDto model)
        {
            var lockedOut = !model.Active;
            var user = await _userManager.FindByIdAsync(model.Id!) ?? throw new Exception($"The application user [{model.Id}] was not found.");
            user.FullName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.Email;
            user.LockoutEnabled = lockedOut;
            user.LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddDays(60) : null;
            await SetUserRole(user, model.Role);
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> UpdateThirdParty(string userId, LuminaUserInfo model)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception($"The application user [{userId}] was not found.");
            user.LuminaUserInfo = model;
            return await _userManager.UpdateAsync(user);
        }

        public async Task AddUserToRole(LuminaUser user, string role)
        {
            await SetUserRole(user, role);
        }

        public async Task SetUserRole(LuminaUser user, string? role)
        {
            foreach (var existingRole in Roles)
            {
                if (await _userManager.IsInRoleAsync(user, existingRole))
                    await _userManager.RemoveFromRoleAsync(user, existingRole);
            }

            if (Roles.Contains(role))
            {
                await _userManager.AddToRoleAsync(user, role!);
            }
        }

        public async Task<IdentityResult> SetUserActive(string userId, bool active)
        {
            bool lockedOut = !active;
            var user = await _userManager.FindByIdAsync(userId!) ?? throw new Exception($"Application user not found {userId}.");
            user.LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddDays(60) : null;
            user.LockoutEnabled = lockedOut;
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception("User not found");
            return await _userManager.DeleteAsync(user);
        }
    }
}
