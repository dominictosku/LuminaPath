using Data.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models.Base
{
	public class Quest : IBasicInfo
	{
		public int Id { get; set; }
		[Required(ErrorMessage = "Please enter a description")]
		public string? Description { get; set; }
		public bool HasStartDate { get; set; } = false;
		public DateTime StartDate { get; set; } = DateTime.Now;
		public string? Location { get; set; }
		public LuminaUser? Owner { get; set; }
		public List<LuminaUser>? Users { get; set; }
	}
}
