using Domain.Common.Entities;
using Domain.Models;

namespace Application.Common.Interfaces
{
    public interface ITokenGenerator
	{
		public Task<AuthenticationResponse> CreateToken(LuminaUser user);
	}
}
