namespace LuminaPath.Core.Dtos
{
    public class UserGridItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;
        public string RoleLabel => string.IsNullOrWhiteSpace(Role) ? "No role" : Role;
        public string StatusLabel => IsLockedOut ? "Locked" : "Active";
        public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName[..1].ToUpperInvariant();
    }
}
