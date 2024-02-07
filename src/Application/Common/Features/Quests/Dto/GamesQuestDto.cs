using Domain.Common.Entities.Base;
using Domain.Common.Interfaces;
using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.Common.Features.Quests.Dto
{
	public class GamesQuestDto : IBasicInfo
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
