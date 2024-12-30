using LuminaPath.Core.Common.Enums;
using LuminaPath.Core.Common.Features.Gaming.Dto;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Third_Party;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using static LuminaPath.Core.Common.Entities.PSN.PSNProfile;
using static LuminaPath.Core.Common.Entities.PSN.PSNTitles;
using static LuminaPath.Core.Common.Entities.PSN.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.Third_Party
{
    public class PSNService
    {
        string bearerToken = string.Empty;

        public void SetBearer(string token) => bearerToken = token;

        public async Task<string> GetAuthenticationToken(string npsso)
        {
            // login and get token from https://ca.account.sony.com/api/v1/ssocookie
            if (string.IsNullOrWhiteSpace(npsso))
            {
                Console.WriteLine("Error: NPSSO token is required.");
                return null;
            }

            string authorizationUrl = "https://ca.account.sony.com/api/authz/v3/oauth/authorize";
            string tokenUrl = "https://ca.account.sony.com/api/authz/v3/oauth/token";
            using HttpClient httpClient = new HttpClient();

            var queryParams = new Dictionary<string, string>
            {
                { "access_type", "offline" },
                { "client_id", "09515159-7237-4370-9b40-3806e67c0891" },
                { "response_type", "code" },
                { "scope", "psn:mobile.v2.core psn:clientapp" },
                { "redirect_uri", "com.scee.psxandroid.scecompcall://redirect" }
            };

            var requestUri = authorizationUrl + "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Add("Cookie", $"npsso={npsso}");

                var response = await httpClient.SendAsync(request);

                if (!response.Headers.Location.Query.StartsWith("?code=v3"))
                {
                    Console.WriteLine("Error: Check NPSSO token.");
                    return null;
                }

                var queryParamsFromResponse = HttpUtility.ParseQueryString(response.Headers.Location.Query);
                string code = queryParamsFromResponse["code"];

                var formContent = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", "com.scee.psxandroid.scecompcall://redirect"),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("token_format", "jwt")
                });

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = formContent
                };

                tokenRequest.Headers.Add("Authorization", "Basic MDk1MTUxNTktNzIzNy00MzcwLTliNDAtMzgwNmU2N2MwODkxOnVjUGprYTV0bnRCMktxc1A=");

                var tokenResponse = await httpClient.SendAsync(tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: Unable to obtain Authentication Token.");
                    return null;
                }

                var jsonResponse = await tokenResponse.Content.ReadAsStringAsync();
                var tokenObject = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

                if (!string.IsNullOrWhiteSpace(tokenObject?.AccessToken))
                {
                    Console.WriteLine("Authentication Token successfully granted.");
                    return tokenObject.AccessToken;
                }
                else
                {
                    Console.WriteLine("Error: Unable to obtain Authentication Token.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public List<MyGameDto> ConvertPSNTitles(GameData gameData)
        {
            List<MyGameDto> list = new List<MyGameDto>();
            var titles = gameData.Titles;
            foreach (var title in titles)
            {
                var plattform = title.Category.ToLower().Contains("ps4") ? Plattforms.Playstation4 : Plattforms.Playstation5;
                var gameName = plattform == Plattforms.Playstation4 ? title.Name + " PS4" : title.Name;
                var game = new MyGameDto()
                {
                    MyGameInfo = new()
                    {
                        FirstPlayed = title.FirstPlayedDateTime,
                        LastPlayed = title.LastPlayedDateTime,
                        TrackedHours = DurationToHours(title.PlayDuration)
                    },
                    Game = new(){
                        Name = gameName,
                        Source = "PSN",
                        Plattforms = plattform,
                        GameInfo = new(){
                            PsnId = title.TitleId,
                        }
                    },
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

        public async Task<GameData> GetTitles(int offset, string accountId)
        {
            var input = $"?limit=200&offset={offset}";
            string apiUrl = $"https://m.np.playstation.com/api/gamelist/v2/users/{accountId}/titles{input}";
            var responseData = await MakeRequest(apiUrl);
            GameData gameData = JsonSerializer.Deserialize<GameData>(responseData);
            return gameData;

        }

        public async Task<TrophyProfileData> GetUserProfileTrophy(string accountId)
        {
            string apiUrl = $"https://m.np.playstation.com/api/trophy/v1/users/{accountId}/trophySummary";
            var responseData = await MakeRequest(apiUrl);
            TrophyProfileData trophyData = JsonSerializer.Deserialize<TrophyProfileData>(responseData);
            return trophyData;
        }


        public async Task<TrophyData> GetUserTrophyTitles(string accountId)
        {
            string apiUrl = $"https://m.np.playstation.com/api/trophy/v1/users/{accountId}/trophyTitles";
            var responseData = await MakeRequest(apiUrl);
            TrophyData trophyData = JsonSerializer.Deserialize<TrophyData>(responseData);
            return trophyData;
        }

        private async Task<string> MakeRequest(string apiUrl)
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

        private static double DurationToHours(string duration)
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
            double totalHours = hours + (minutes / 60.0) + (seconds / 3600.0);
            return Math.Round(totalHours, 2); // Return rounded value (to 2 decimal places)
        }

        private static DateTime FormatDate(string dateStr)
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

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
