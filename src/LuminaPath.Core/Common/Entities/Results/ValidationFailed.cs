using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Common.Entities.Results
{
	public record ValidationFailed(IEnumerable<ValidationFailure> errorMessage)
	{
		public ValidationFailed(ValidationFailure error) : this(new[] { error })
		{

		}
	}
}
