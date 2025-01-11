using System.Text.Json.Serialization;

namespace LuminaPath.Core.Entities.PSN
{
    public class PSNProfile
    {
        public class ProfileData
        {
            [JsonPropertyName("profile")]
            public Profile Profile { get; set; }
        }

        public class Profile
        {
            [JsonPropertyName("onlineId")]
            public string OnlineId { get; set; }

            [JsonPropertyName("accountId")]
            public string AccountId { get; set; }
        }
    }
}
