using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Models
{
    public class Game : Media
    {
        [Display(Name = "Platform")]
        public Platforms Platforms { get; set; }
        [Display(Name = "Estimated playtime")]
        public int? Playtime { get; set; }

        public int? ParentGameId { get; set; }
        public Game? ParentGame { get; set; }
        public List<Game>? Dlcs { get; set; }

        public List<MyGame>? MyGames { get; set; }
        public List<GameAchievement>? Achievements { get; set; }
    }
}
