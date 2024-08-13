using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Common.Enums
{
	[Flags]
	public enum Plattforms
	{
		[Display(Name = "Playstation")]
		Playstation = 1,
		[Display(Name = "Switch")]
		Switch = 2,
		[Display(Name = "PC")]
		PC = 4,
		[Display(Name = "XBOX")]
		XBOX = 8
	}
}
