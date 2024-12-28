using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models.Base
{
	public abstract class MyMedia : IBasicInfo
	{
		public int Id { get; set; }
		[Range(1, 10)]
		public short? Rating { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public int? TimeSpend { get; set; }
		public string? LuminaUserId { get; set; }
		public LuminaUser? LuminaUser { get; set; }
	}
}
