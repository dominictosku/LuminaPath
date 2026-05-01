using System.Text.Json;

namespace LuminaPath.Infrastructure.Services
{
    public class FileSystemService
    {
        private string filePath => Path.Combine(AppContext.BaseDirectory, "settings.json");
        public Dictionary<string, string> ReadSettings()
        {
            if (!File.Exists(filePath))
            {
                var defaultSettings = new
                {
                    PSNBearer = ""
                };
                // Serialize the object to JSON format
                string newJson = JsonSerializer.Serialize(defaultSettings);
                // Write the JSON to the file
                File.WriteAllText(filePath, newJson);
            }
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return data ?? new Dictionary<string, string>();
        }

        public string GetBearer()
        {
            var data = ReadSettings();
            data.TryGetValue("PSNBearer", out var bearer);
            return bearer ?? "";
        }

        public void WriteBearer(string bearer)
        {
            var data = ReadSettings();

            // Writing
            data["PSNBearer"] = bearer;
            var updatedJson = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, updatedJson);
        }
    }
}
