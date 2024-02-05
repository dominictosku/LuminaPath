using System.ComponentModel.DataAnnotations;

namespace Domain.Common.Entities
{
    public class UserCredentials
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
