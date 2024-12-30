using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;
using LuminaPath.Core.Models.Base;
using LuminaPath.Core.Models.Third_Party;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuminaPath.Core.Models
{
    public class MyGame : MyMedia, IMyMedia
    {
        [NotMapped]
        public int MediaId => GameId;
        public GameStatus Status { get; set; }

        [Required(ErrorMessage = "No {0} was choosen")]
        [Display(Name = "Game Id")]
        public int GameId { get; set; }

        public Game? Game { get; set; }
        public MyGameInfo? MyGameInfo { get; set; }
    }
}
