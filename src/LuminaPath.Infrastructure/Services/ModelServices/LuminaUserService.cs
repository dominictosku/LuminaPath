using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Auditing;
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
        private readonly AuditLogService _auditLog;

        public LuminaUserService(
            IDbContextFactory<LuminaPathDbContext> dbContextFactory,
            UserManager<LuminaUser> userManager,
            AuditLogService auditLog)
        {
            _dbContextFactory = dbContextFactory;
            _userManager = userManager;
            _auditLog = auditLog;
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
                var failure = IdentityResult.Failed(new IdentityError { Description = "Password is required." });
                await AuditUserCreateFailure(model, failure);
                return failure;
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
                    await _auditLog.RecordAsync(new AuditLogEntry
                    {
                        Category = AuditCategories.Admin,
                        Action = AuditActions.UserCreated,
                        Outcome = AuditOutcomes.Success,
                        TargetType = "User",
                        TargetId = user.Id,
                        TargetName = user.Email,
                        Changes = AuditLogService.Changes(
                            ("Email", null, user.Email),
                            ("FullName", null, user.FullName),
                            ("PhoneNumber", null, user.PhoneNumber),
                            ("Role", null, await GetUserRole(user)),
                            ("Active", null, !lockedOut)),
                        Metadata = new { source = "AdminUsers" }
                    });
                }
            }
            else
            {
                await AuditUserCreateFailure(model, state);
            }
            return state;
        }

        public async Task<IdentityResult> UpdateUser(UserDto model)
        {
            var lockedOut = !model.Active;
            var user = await _userManager.FindByIdAsync(model.Id!) ?? throw new Exception($"The application user [{model.Id}] was not found.");
            var oldEmail = user.Email;
            var oldFullName = user.FullName;
            var oldPhone = user.PhoneNumber;
            var oldActive = !(user.LockoutEnabled && user.LockoutEnd != null && user.LockoutEnd >= DateTimeOffset.UtcNow);
            var oldRole = await GetUserRole(user);

            user.FullName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.Email;
            user.LockoutEnabled = lockedOut;
            user.LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddDays(60) : null;
            await SetUserRole(user, model.Role);
            var result = await _userManager.UpdateAsync(user);
            var newRole = await GetUserRole(user);

            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = oldRole != newRole ? AuditActions.RoleChanged : AuditActions.UserUpdated,
                Outcome = result.Succeeded ? AuditOutcomes.Success : AuditOutcomes.Failure,
                TargetType = "User",
                TargetId = user.Id,
                TargetName = user.Email,
                Changes = AuditLogService.Changes(
                    ("Email", oldEmail, user.Email),
                    ("FullName", oldFullName, user.FullName),
                    ("PhoneNumber", oldPhone, user.PhoneNumber),
                    ("Role", oldRole, newRole),
                    ("Active", oldActive, model.Active)),
                Metadata = new { source = "AdminUsers" },
                ErrorMessage = result.Succeeded ? null : string.Join(", ", result.Errors.Select(error => error.Description))
            });

            return result;
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
            var oldActive = !(user.LockoutEnabled && user.LockoutEnd != null && user.LockoutEnd >= DateTimeOffset.UtcNow);
            user.LockoutEnd = lockedOut ? DateTimeOffset.UtcNow.AddDays(60) : null;
            user.LockoutEnabled = lockedOut;
            var result = await _userManager.UpdateAsync(user);
            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = active ? AuditActions.UserActivated : AuditActions.UserLocked,
                Outcome = result.Succeeded ? AuditOutcomes.Success : AuditOutcomes.Failure,
                TargetType = "User",
                TargetId = user.Id,
                TargetName = user.Email,
                Changes = AuditLogService.Changes(("Active", oldActive, active)),
                Metadata = new { source = "AdminUsers" },
                ErrorMessage = result.Succeeded ? null : string.Join(", ", result.Errors.Select(error => error.Description))
            });
            return result;
        }

        public async Task<IdentityResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception("User not found");
            var role = await GetUserRole(user);
            var result = await _userManager.DeleteAsync(user);
            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.UserDeleted,
                Outcome = result.Succeeded ? AuditOutcomes.Success : AuditOutcomes.Failure,
                TargetType = "User",
                TargetId = user.Id,
                TargetName = user.Email,
                Metadata = new
                {
                    source = "AdminUsers",
                    role,
                    fullName = user.FullName
                },
                ErrorMessage = result.Succeeded ? null : string.Join(", ", result.Errors.Select(error => error.Description))
            });
            return result;
        }

        private async Task AuditUserCreateFailure(UserDto model, IdentityResult result)
        {
            await _auditLog.RecordAsync(new AuditLogEntry
            {
                Category = AuditCategories.Admin,
                Action = AuditActions.UserCreated,
                Outcome = AuditOutcomes.Failure,
                TargetType = "User",
                TargetName = model.Email,
                Metadata = new
                {
                    source = "AdminUsers",
                    errors = result.Errors.Select(error => error.Code).ToArray()
                },
                ErrorMessage = string.Join(", ", result.Errors.Select(error => error.Description))
            });
        }

        private async Task<string> GetUserRole(LuminaUser user)
        {
            foreach (var role in Roles)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    return role;
                }
            }

            return string.Empty;
        }
    }
}
