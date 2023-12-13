using Core.Models.Base;

namespace Core.Classes
{
	public class MediaFilter
	{
		public string? SearchString { get; set; }
		public ProgressionStatus Status { get; set; }
		public Paging Paging { get; set; } = new Paging();
	}
}
