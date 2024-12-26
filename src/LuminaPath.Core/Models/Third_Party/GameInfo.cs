using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Core.Models.Third_Party
{
    public class GameInfo
    {
        public int Id { get; set; }
        public Game Game { get; set; }
        public int GameId { get; set; }
        public string PsnId { get; set; }
        public double TrackedHours { get; set; }
        public DateTime FirstPlayed { get; set; }
        public DateTime LastPlayed { get; set; }
    }
}
