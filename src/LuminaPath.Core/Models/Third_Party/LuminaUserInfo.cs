namespace LuminaPath.Core.Models.Third_Party
{
    public class LuminaUserInfo
    {
        public int Id { get; set; }
        public LuminaUser? User { get; set; }
        public string UserId { get; set; } = string.Empty;

        #region PSN
        public string PSNOnlineId { get; set; } = string.Empty;

        public string PSNAccountId { get; set; } = string.Empty;

        public int PSNTrophyLevel { get; set; }

        public int PSNBronze { get; set; }

        public int PSNSilver { get; set; }

        public int PSNGold { get; set; }

        public int PSNPlatinum { get; set; }
        #endregion
    }
}
