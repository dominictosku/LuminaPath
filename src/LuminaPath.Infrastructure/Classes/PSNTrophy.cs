using System.Text.Json.Serialization;

namespace LuminaPath.Infrastructure.Classes
{
    public class PSNTrophy
    {
        public class TrophyData
        {
            [JsonPropertyName("trophyTitles")]
            public List<TrophyTitle> TrophyTitles { get; set; }

            [JsonPropertyName("totalItemCount")]
            public int TotalItemCount { get; set; }
        }

        public class TrophyTitle
        {
            [JsonPropertyName("npServiceName")]
            public string NpServiceName { get; set; }

            [JsonPropertyName("npCommunicationId")]
            public string NpCommunicationId { get; set; }

            [JsonPropertyName("trophySetVersion")]
            public string TrophySetVersion { get; set; }

            [JsonPropertyName("trophyTitleName")]
            public string TrophyTitleName { get; set; }

            [JsonPropertyName("trophyTitleIconUrl")]
            public string TrophyTitleIconUrl { get; set; }

            [JsonPropertyName("trophyTitlePlatform")]
            public string TrophyTitlePlatform { get; set; }

            [JsonPropertyName("hasTrophyGroups")]
            public bool HasTrophyGroups { get; set; }

            [JsonPropertyName("definedTrophies")]
            public DefinedTrophies DefinedTrophies { get; set; }

            [JsonPropertyName("progress")]
            public int Progress { get; set; }

            [JsonPropertyName("earnedTrophies")]
            public EarnedTrophies EarnedTrophies { get; set; }

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
