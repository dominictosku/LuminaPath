using Data.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Classes
{
	public class MediaFIlter
	{
		public string? SearchString { get; set; }
		public int PageIndex { get; set; }
		public ProgressionStatus Status { get; set; }

		public MediaFIlter()
		{
			SearchString = "";
			PageIndex = 1;
			Status = ProgressionStatus.Open;
		}
	}
}
