using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models.ThirdParty;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuminaPath.Infrastructure.Services.ModelServices
{
    public partial class LuminaUserService
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

        public async Task<IdentityResult> CreateUser(UserDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                var failure = IdentityResult.Failed(new IdentityError { Description = "Password is required." });
                await AuditUserCreateFailure(model, failure);
                return failure;
            }

            var applicationUser = new LuminaUser
            {
                FullName = model.UserName,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                // Admin-created users skip the email-confirmation step;
                // the admin attesting in the form is the trust signal.
                EmailConfirmed = true,
                // IsActive is the sole sign-in gate now. Admins choose
                // it in the form — the LuminaUserManager admin-approval
                // gate doesn't trigger here because the request is
                // authenticated (the admin is logged in).
                IsActive = model.Active
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
                            ("Active", null, model.Active)),
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
            var user = await _userManager.FindByIdAsync(model.Id!) ?? throw new Exception($"The application user [{model.Id}] was not found.");
            var oldEmail = user.Email;
            var oldFullName = user.FullName;
            var oldPhone = user.PhoneNumber;
            var oldActive = user.IsActive;
            var oldRole = await GetUserRole(user);

            user.FullName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.Email;
            user.IsActive = model.Active;
            // Clearing LockoutEnd on activate gives admins a single
            // "kick the user back in" toggle that covers both manual
            // deactivation AND the auto-lockout window after too many
            // failed attempts. Without this, an admin re-activating a
            // user who got auto-locked from failed sign-ins would still
            // see the user blocked until LockoutEnd expired.
            if (model.Active)
            {
                user.LockoutEnd = null;
            }
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
            var user = await _userManager.FindByIdAsync(userId!) ?? throw new Exception($"Application user not found {userId}.");
            var oldActive = user.IsActive;
            user.IsActive = active;
            // Activating also clears any auto-lockout that may be in
            // effect from failed sign-in attempts — see UpdateUser for
            // the reasoning.
            if (active)
            {
                user.LockoutEnd = null;
            }
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
