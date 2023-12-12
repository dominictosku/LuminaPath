using Data.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Classes
{
	public class Paging
	{
		public int PageIndex { get; set; } = 1;
		public int Count { get; set; }
	}
}
