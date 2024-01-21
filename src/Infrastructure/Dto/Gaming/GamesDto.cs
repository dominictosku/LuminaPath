using Core.Models.Base;
using Core.Models.Gaming;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Dto.Gaming
{
    public class GamesDto : Media
    {
        [Display(Name = "Plattform")]
        public Plattforms Plattforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }
        public MyGameDto? MyGames { get; set; }
    }
}
