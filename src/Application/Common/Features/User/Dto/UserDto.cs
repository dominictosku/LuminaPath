using System.ComponentModel.DataAnnotations;

namespace Application.Common.Features.User.Dto
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
        public string PhoneNumber { get; set; } = string.Empty;
        public bool LockoutEnabled { get; set; }
    }
}
