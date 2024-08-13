using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Common.Entities.Results
{
	public record FailedResult(IEnumerable<string> errorMessage)
	{
		public FailedResult(string error) : this(new[] { error })
		{

		}
	}
}
