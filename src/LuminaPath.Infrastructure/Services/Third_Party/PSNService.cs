using LuminaPath.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static TestPSN.Classes.PSNProfile;
using static TestPSN.Classes.PSNTitles;
using static TestPSN.Classes.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.Third_Party
{
    public class PSNService
    {
        string bearerToken = string.Empty;

        public static double DurationToHours(string duration)
        {
            // Regex to extract hours, minutes, and seconds
            var match = Regex.Match(duration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
            if (!match.Success)
            {
                return 0; // Return 0 if the duration format is invalid
            }

            // Extract and parse hours, minutes, and seconds
            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            // Convert the total time to hours
            double totalHours = hours + minutes / 60.0 + seconds / 3600.0;
            return Math.Round(totalHours, 2); // Return rounded value (to 2 decimal places)
        }

        public static DateTime FormatDate(string dateStr)
        {
            try
            {
                // Parse ISO 8601 date string
                DateTime dt = DateTime.Parse(dateStr, null, DateTimeStyles.RoundtripKind);
                return dt;
            }
            catch (FormatException)
            {
                return new DateTime(); // Return the original string if parsing fails
            }
        }

        public void SetBearer(string token) => bearerToken = token;

        public List<Game> ConvertPSNTitles(GameData gameData)
        {
            List<Game> list = new List<Game>();
            var titles = gameData.Titles;
            foreach (var title in titles) 
            {
                var game = new Game()
                {
                    Name = title.Name,
                    Source = "PSN",
                    GameInfo = { 
                        FirstPlayed = title.FirstPlayedDateTime,
                        LastPlayed = title.LastPlayedDateTime,
                        PsnId = title.TitleId,
                        TrackedHours = DurationToHours(title.PlayDuration)
                    }
                };
                list.Add(game);
            }
            return list;
        }

        public async Task<ProfileData> GetProfile(string userName)
        {
            string apiUrl = $"https://us-prof.np.community.playstation.net/userProfile/v1/users/{userName}/profile2?fields=accountId,onlineId,currentOnlineId";
            var responseData = await MakeRequest(apiUrl);
            ProfileData profile = JsonSerializer.Deserialize<ProfileData>(responseData);
            return profile;
        }

        public async Task<ProfileData> GetMyProfile()
        {
            string apiUrl = $"https://us-prof.np.community.playstation.net/userProfile/v1/users/me/profile2?fields=accountId,onlineId,currentOnlineId";
            var responseData = await MakeRequest(apiUrl);
            ProfileData profile = JsonSerializer.Deserialize<ProfileData>(responseData);
            return profile;
        }

        public async Task<GameData> GetMyTitles(int offset)
        {
            var input = $"?limit=200&offset={offset}";
            string apiUrl = $"https://m.np.playstation.com/api/gamelist/v2/users/me/titles{input}";
            var responseData = await MakeRequest(apiUrl);
            GameData gameData = JsonSerializer.Deserialize<GameData>(responseData);
            return gameData;

        }


        public async Task<TrophyData> GetMyTrophies()
        {
            string apiUrl = "https://m.np.playstation.com/api/trophy/v1/users/me/trophyTitles";
            var responseData = await MakeRequest(apiUrl);
            TrophyData trophyData = JsonSerializer.Deserialize<TrophyData>(responseData);
            return trophyData;
        }

        public async Task<string> MakeRequest(string apiUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Add the Authorization header with the bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    // Send GET request
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                        return responseData;
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        string errorData = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Error Details:");
                        Console.WriteLine(errorData);
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred:");
                Console.WriteLine(ex.Message);
                return "";
            }
        }
    }
}
