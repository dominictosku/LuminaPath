using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestPSN.Classes
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
