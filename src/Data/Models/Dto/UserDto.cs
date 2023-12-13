using System.ComponentModel.DataAnnotations;

namespace Core.Models.Dto
{
	public class UserDto
	{
		[Required]
		public string UserName { get; set; } = string.Empty;
		[Required]
		public string Password { get; set; } = string.Empty;
		[Required]
		public string Email { get; set; } = string.Empty;
	}
}
