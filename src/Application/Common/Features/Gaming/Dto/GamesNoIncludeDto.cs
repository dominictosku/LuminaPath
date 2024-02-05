using Domain.Common.Entities.Base;
using Domain.Common.Enums;
using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.Common.Features.Gaming.Dto
{
    public class GamesNoIncludeDto : Media
    {
        [Display(Name = "Plattform")]
        public Plattforms Plattforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }
    }
}
