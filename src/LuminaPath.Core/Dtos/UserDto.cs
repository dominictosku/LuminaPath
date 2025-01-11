using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
