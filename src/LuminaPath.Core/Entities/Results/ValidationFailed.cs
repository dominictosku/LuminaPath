using Microsoft.IdentityModel.Tokens;

namespace LuminaPath.Core.Entities.Results
{
    public record ValidationFailed(IEnumerable<ValidationFailure> errorMessage)
    {
        public ValidationFailed(ValidationFailure error) : this(new[] { error })
        {

        }
    }
}
