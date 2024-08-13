using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.MauiClientApp.Models
{
	public class Game : Core.Models.Game
	{
		[PrimaryKey, AutoIncrement]
		public override int Id { get; set; }
	}
}
