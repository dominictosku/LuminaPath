using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;

namespace LuminaPath.MauiClientApp.Models
{
	public class LocalMyGame : IBasicInfo
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		[Range(1, 10)]
		public short? Rating { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public ProgressionStatus Status { get; set; }
		public int? TimeSpend { get; set; }

		[Required(ErrorMessage = "No {0} was choosen")]
		[Display(Name = "Game")]
		public int MediaId => GameId;

		[Indexed]
		public int GameId { get; set; }
	}
}
