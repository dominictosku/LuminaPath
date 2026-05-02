using System.Text.Json.Serialization;

namespace LuminaPath.Core.Entities.PSN
{
    public class PSNTrophy
    {
        public class TrophyData
        {
            [JsonPropertyName("trophyTitles")]
            public List<TrophyTitle> TrophyTitles { get; set; } = [];

            [JsonPropertyName("totalItemCount")]
            public int TotalItemCount { get; set; }
        }

        public class TrophyProfileData
        {
            [JsonPropertyName("accountId")]
            public string AccountId { get; set; } = string.Empty;

            [JsonPropertyName("trophyLevel")]
            public int TrophyLevel { get; set; }

            [JsonPropertyName("trophyPoint")]
            public int TrophyPoint { get; set; }

            [JsonPropertyName("trophyLevelBasePoint")]
            public int TrophyLevelBasePoint { get; set; }

            [JsonPropertyName("trophyLevelNextPoint")]
            public int TrophyLevelNextPoint { get; set; }

            [JsonPropertyName("progress")]
            public int Progress { get; set; }

            [JsonPropertyName("tier")]
            public int Tier { get; set; }

            [JsonPropertyName("earnedTrophies")]
            public EarnedTrophies EarnedTrophies { get; set; } = new();
        }


        public class TrophyTitle
        {
            [JsonPropertyName("npServiceName")]
            public string NpServiceName { get; set; } = string.Empty;

            [JsonPropertyName("npCommunicationId")]
            public string NpCommunicationId { get; set; } = string.Empty;

            [JsonPropertyName("trophySetVersion")]
            public string TrophySetVersion { get; set; } = string.Empty;

            [JsonPropertyName("trophyTitleName")]
            public string TrophyTitleName { get; set; } = string.Empty;

            [JsonPropertyName("trophyTitleIconUrl")]
            public string TrophyTitleIconUrl { get; set; } = string.Empty;

            [JsonPropertyName("trophyTitlePlatform")]
            public string TrophyTitlePlatform { get; set; } = string.Empty;

            [JsonPropertyName("hasTrophyGroups")]
            public bool HasTrophyGroups { get; set; }

            [JsonPropertyName("definedTrophies")]
            public DefinedTrophies DefinedTrophies { get; set; } = new();

            [JsonPropertyName("progress")]
            public int Progress { get; set; }

            [JsonPropertyName("earnedTrophies")]
            public EarnedTrophies EarnedTrophies { get; set; } = new();

            [JsonPropertyName("hiddenFlag")]
            public bool HiddenFlag { get; set; }

            [JsonPropertyName("lastUpdatedDateTime")]
            public DateTime LastUpdatedDateTime { get; set; }
        }

        public class DefinedTrophies
        {
            [JsonPropertyName("bronze")]
            public int Bronze { get; set; }

            [JsonPropertyName("silver")]
            public int Silver { get; set; }

            [JsonPropertyName("gold")]
            public int Gold { get; set; }

            [JsonPropertyName("platinum")]
            public int Platinum { get; set; }
        }

        public class EarnedTrophies
        {
            [JsonPropertyName("bronze")]
            public int Bronze { get; set; }

            [JsonPropertyName("silver")]
            public int Silver { get; set; }

            [JsonPropertyName("gold")]
            public int Gold { get; set; }

            [JsonPropertyName("platinum")]
            public int Platinum { get; set; }
        }
    }
}
