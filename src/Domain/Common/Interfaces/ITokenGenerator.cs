using Domain.Entities;
using Domain.Models;

namespace Domain.Common.Interfaces
{
	public interface ITokenGenerator
	{
		public Task<AuthenticationResponse> CreateToken(LuminaUser user);
	}
}
