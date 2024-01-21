using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
	public class UserCredentials
	{
		[Required]
		public string UserName { get; set; } = string.Empty;
		[Required]
		public string Password { get; set; } = string.Empty;
	}
}
