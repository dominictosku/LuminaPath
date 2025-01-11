using LuminaPath.Core.Entities;
using LuminaPath.Core.Models;

namespace LuminaPath.Core.Interfaces
{
    public interface ITokenGenerator
    {
        public Task<AuthenticationResponse> CreateToken(LuminaUser user);
    }
}
