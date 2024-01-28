using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Base
{
	public class Quest : IBasicInfo
	{
		public int Id { get; set; }
		[Required(ErrorMessage = "Please enter a description")]
		public string? Description { get; set; }
		public bool HasStartDate { get; set; } = false;
		public DateTime? StartDate { get; set; }
		public string? Location { get; set; }
		public LuminaUser? Owner { get; set; }
	}
}
