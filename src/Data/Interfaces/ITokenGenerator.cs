using Core.Entities;
using Core.Models;

namespace Core.Interfaces
{
	public interface ITokenGenerator
	{
		public Task<AuthenticationResponse> CreateToken(LuminaUser user);
	}
}
