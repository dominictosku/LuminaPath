namespace LuminaPath.Core.Models.Third_Party
{
    public class MyGameInfo
    {
        public int Id { get; set; }
        public MyGame Game { get; set; }
        public int MyGameId { get; set; }
        public double TrackedHours { get; set; }
        public DateTime FirstPlayed { get; set; }
        public DateTime LastPlayed { get; set; }
    }
}
