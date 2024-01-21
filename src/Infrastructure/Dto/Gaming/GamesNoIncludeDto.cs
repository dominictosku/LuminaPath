using Core.Models.Base;
using Core.Models.Gaming;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Dto.Gaming
{
    public class GamesNoIncludeDto : Media
    {
        [Display(Name = "Plattform")]
        public Plattforms Plattforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }
    }
}
