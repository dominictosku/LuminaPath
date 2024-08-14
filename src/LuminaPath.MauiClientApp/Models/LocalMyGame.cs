using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LuminaPath.Core.Common.Enums;

namespace LuminaPath.MauiClientApp.Models
{
	public enum ProgressionStatus
	{
		Planned,
		InProgress,
		Completed,
	}

	public class LocalMyGame
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
