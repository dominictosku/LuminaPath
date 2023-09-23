using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models.Base;

namespace Data.Models
{
    public enum Plattforms
    {
        [Display(Name = "Playstation")]
        Playstation,
        [Display(Name = "Switch")]
        Switch,
        [Display(Name = "PC")]
        PC,
        [Display(Name = "XBOX")]
        XBOX
    }
    public partial class Game : Media
    {
        [Display(Name = "Plattform")]
		public Plattforms Plattforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }
        public List<MyGame>? MyGames { get; set; }
    }
}
