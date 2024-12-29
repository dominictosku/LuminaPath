using LuminaPath.Core.Common.Entities;
using LuminaPath.Core.Models;

namespace LuminaPath.Core.Common.Interfaces
{
    public interface ITokenGenerator
    {
        public Task<AuthenticationResponse> CreateToken(LuminaUser user);
    }
}
