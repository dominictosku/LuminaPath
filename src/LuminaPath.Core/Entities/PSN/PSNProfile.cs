using System.Text.Json.Serialization;

namespace LuminaPath.Core.Entities.PSN
{
    public class PSNProfile
    {
        public class ProfileData
        {
            [JsonPropertyName("profile")]
            public Profile Profile { get; set; } = new();
        }

        public class Profile
        {
            [JsonPropertyName("onlineId")]
            public string OnlineId { get; set; } = string.Empty;

            [JsonPropertyName("accountId")]
            public string AccountId { get; set; } = string.Empty;
        }
    }
}
