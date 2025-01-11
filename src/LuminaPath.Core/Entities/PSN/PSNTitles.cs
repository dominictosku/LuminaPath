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
            public string TitleId { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("localizedName")]
            public string LocalizedName { get; set; }

            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; }

            [JsonPropertyName("localizedImageUrl")]
            public string LocalizedImageUrl { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("service")]
            public string Service { get; set; }

            [JsonPropertyName("playCount")]
            public int PlayCount { get; set; }

            [JsonPropertyName("firstPlayedDateTime")]
            public DateTime FirstPlayedDateTime { get; set; }

            [JsonPropertyName("lastPlayedDateTime")]
            public DateTime LastPlayedDateTime { get; set; }

            [JsonPropertyName("playDuration")]
            public string PlayDuration { get; set; }
        }

        public class Concept
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("media")]
            public string Media { get; set; }

            [JsonPropertyName("genres")]
            public string Genres { get; set; }

            [JsonPropertyName("localizedName")]
            public string LocalizedName { get; set; }

            [JsonPropertyName("country")]
            public string Country { get; set; }

            [JsonPropertyName("language")]
            public string Language { get; set; }
        }

        public class Media
        {
            [JsonPropertyName("audios")]
            public string Audios { get; set; }

            [JsonPropertyName("videos")]
            public string Videos { get; set; }

            [JsonPropertyName("images")]
            public string Images { get; set; }
        }
    }
}
