using System.Text.Json.Serialization;

namespace LuminaPath.Core.Entities.PSN
{
    public class PSNTitles
    {
        public class GameData
        {
            [JsonPropertyName("titles")]
            public List<Title> Titles { get; set; } = new();

            [JsonPropertyName("nextOffset")]
            public int? NextOffset { get; set; }

            [JsonPropertyName("previousOffset")]
            public int PreviousOffset { get; set; }

            [JsonPropertyName("totalItemCount")]
            public int TotalItemCount { get; set; }
        }

        public class Title
        {
            [JsonPropertyName("titleId")]
            public string TitleId { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("localizedName")]
            public string LocalizedName { get; set; } = string.Empty;

            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; } = string.Empty;

            [JsonPropertyName("localizedImageUrl")]
            public string LocalizedImageUrl { get; set; } = string.Empty;

            [JsonPropertyName("category")]
            public string Category { get; set; } = string.Empty;

            [JsonPropertyName("service")]
            public string Service { get; set; } = string.Empty;

            [JsonPropertyName("playCount")]
            public int PlayCount { get; set; }

            [JsonPropertyName("firstPlayedDateTime")]
            public DateTime FirstPlayedDateTime { get; set; }

            [JsonPropertyName("lastPlayedDateTime")]
            public DateTime LastPlayedDateTime { get; set; }

            [JsonPropertyName("playDuration")]
            public string PlayDuration { get; set; } = string.Empty;
        }

        public class Concept
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("media")]
            public string Media { get; set; } = string.Empty;

            [JsonPropertyName("genres")]
            public string Genres { get; set; } = string.Empty;

            [JsonPropertyName("localizedName")]
            public string LocalizedName { get; set; } = string.Empty;

            [JsonPropertyName("country")]
            public string Country { get; set; } = string.Empty;

            [JsonPropertyName("language")]
            public string Language { get; set; } = string.Empty;
        }

        public class Media
        {
            [JsonPropertyName("audios")]
            public string Audios { get; set; } = string.Empty;

            [JsonPropertyName("videos")]
            public string Videos { get; set; } = string.Empty;

            [JsonPropertyName("images")]
            public string Images { get; set; } = string.Empty;
        }
    }
}
