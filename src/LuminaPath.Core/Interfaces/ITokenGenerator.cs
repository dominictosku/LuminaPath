using LuminaPath.Core.Entities;

namespace LuminaPath.Core.Interfaces
{
    public interface ITokenGenerator
    {
        public Task<AuthenticationResponse> CreateToken(ILuminaUser user);
    }
}
