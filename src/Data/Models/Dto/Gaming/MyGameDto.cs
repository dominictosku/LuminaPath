using Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Dto.Gaming
{
	public class MyGameDto
	{
		public int Id { get; set; }
		[Range(1, 10)]
		public byte? Rating { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public ProgressionStatus Status { get; set; }
		public int? TimeSpend { get; set; }
		public int GameId { get; set; }
		public GamesNoIncludeDto? Game { get; set; }
	}
}
