using Domain.Common.Entities.Base;
using Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public partial class Game : Media
    {
        [Display(Name = "Plattform")]
        public Plattforms Plattforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }
        public List<MyGame>? MyGames { get; set; }
    }
}
