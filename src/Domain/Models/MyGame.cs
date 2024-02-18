using Domain.Common.Interfaces;
using Domain.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public partial class MyGame : MyMedia, IMyMedia
    {
        [Required(ErrorMessage = "No {0} was choosen")]
        [Display(Name = "Game")]
        [NotMapped]
        public int MediaId => GameId;
        public int GameId { get; set; }
        public Game? Game { get; set; }
    }
}
