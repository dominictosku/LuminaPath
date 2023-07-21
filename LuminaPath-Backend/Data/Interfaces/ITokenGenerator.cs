using Data.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
	public interface ITokenGenerator
	{
		public AuthenticationResponse CreateToken(LuminaUser user);
	}
}
