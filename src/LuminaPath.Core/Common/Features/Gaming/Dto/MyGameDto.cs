using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LuminaPath.Core.Common.Features.Gaming.Dto
{
    public class MyGameDto : IBasicInfo
    {
        public int Id { get; set; }
        [Range(1, 10)]
        public byte? Rating { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public GameStatus Status { get; set; }
        public int? TimeSpend { get; set; }
        public int GameId { get; set; }
        public GamesNoIncludeDto? Game { get; set; }
    }
}
