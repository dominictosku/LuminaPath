namespace LuminaPath.Core.Common.Entities
{
	public class AuthenticationResponse
	{
		public string Token { get; set; } = string.Empty;
		public DateTime Expiration { get; set; }
	}
}
