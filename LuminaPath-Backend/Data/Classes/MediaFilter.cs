using Data.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Classes
{
	public class MediaFilter
	{
		public string? SearchString { get; set; }
		public ProgressionStatus Status { get; set; }
		public Paging Paging { get; set; } = new Paging();
	}
}
