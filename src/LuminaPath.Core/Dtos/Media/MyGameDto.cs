using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models.ThirdParty;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Dtos
{
    public class MyGameDto : IBasicInfo
    {
        public int Id { get; set; }
        [Range(1, 10)]
        public byte? Rating { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public GameStatus Status { get; set; }
        public double? TimeSpend { get; set; }
        public int GameId { get; set; }
        public string? PersonalNotes { get; set; }
        public GamesNoIncludeDto? Game { get; set; }
        public MyGameInfo? MyGameInfo { get; set; } = new();
    }
}
