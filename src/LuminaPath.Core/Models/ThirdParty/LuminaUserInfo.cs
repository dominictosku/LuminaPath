namespace LuminaPath.Core.Models.ThirdParty
{
    public class LuminaUserInfo
    {
        public int Id { get; set; }
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

        #region Steam
        public string SteamId { get; set; } = string.Empty;

        public string SteamPersonaName { get; set; } = string.Empty;

        public string SteamProfileUrl { get; set; } = string.Empty;

        public string SteamAvatarUrl { get; set; } = string.Empty;
        #endregion
    }
}
