using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Models.Third_Party;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.Imports;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using static LuminaPath.Core.Entities.PSN.PSNProfile;
using static LuminaPath.Core.Entities.PSN.PSNTitles;
using static LuminaPath.Core.Entities.PSN.PSNTrophy;

namespace LuminaPath.Infrastructure.Services.Third_Party
{
    public class PSNService(
        HttpClient httpClient,
        GameImportPipeline importPipeline,
        ILogger<PSNService> logger) : IPsnTrophyClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly GameImportPipeline _importPipeline = importPipeline;
        private readonly ILogger<PSNService> _logger = logger;
        string bearerToken = string.Empty;

        public void SetBearer(string token) => bearerToken = token;

        public async Task<string> GetAuthenticationToken(string npsso)
        {
            // login and get token from https://ca.account.sony.com/api/v1/ssocookie
            if (string.IsNullOrWhiteSpace(npsso))
            {
                _logger.LogWarning("NPSSO token is required.");
                return string.Empty;
            }

            string authorizationUrl = "https://ca.account.sony.com/api/authz/v3/oauth/authorize";
            string tokenUrl = "https://ca.account.sony.com/api/authz/v3/oauth/token";

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

                using var response = await _httpClient.SendAsync(request);

                var location = response.Headers.Location;
                if (location?.Query.StartsWith("?code=v3") != true)
                {
                    _logger.LogWarning("Could not authenticate with the provided NPSSO token.");
                    return string.Empty;
                }

                var queryParamsFromResponse = HttpUtility.ParseQueryString(location.Query);
                string code = queryParamsFromResponse["code"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    _logger.LogWarning("PlayStation authentication response did not contain an authorization code.");
                    return string.Empty;
                }

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

                using var tokenResponse = await _httpClient.SendAsync(tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Unable to obtain PlayStation authentication token. Status code: {StatusCode}", tokenResponse.StatusCode);
                    return string.Empty;
                }

                var jsonResponse = await tokenResponse.Content.ReadAsStringAsync();
                var tokenObject = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

                if (!string.IsNullOrWhiteSpace(tokenObject?.AccessToken))
                {
                    _logger.LogInformation("PlayStation authentication token granted.");
                    return tokenObject.AccessToken;
                }
                else
                {
                    _logger.LogWarning("PlayStation token response did not contain an access token.");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayStation authentication failed.");
                return string.Empty;
            }
        }

        public List<MyGameDto> ConvertPSNTitles(GameData gameData)
        {
            List<MyGameDto> list = new List<MyGameDto>();
            var titles = gameData.Titles;
            foreach (var title in titles)
            {
                var platform = title.Category.ToLower().Contains("ps4") ? Platforms.Playstation4 : Platforms.Playstation5;
                var gameName = platform == Platforms.Playstation4 ? title.Name + " PS4" : title.Name;
                var game = new MyGameDto()
                {
                    MyGameInfo = new()
                    {
                        FirstPlayed = title.FirstPlayedDateTime,
                        LastPlayed = title.LastPlayedDateTime,
                        TrackedHours = DurationToHours(title.PlayDuration)
                    },
                    Game = new()
                    {
                        Name = gameName,
                        Source = "PSN",
                        Platforms = platform,
                        PsnId = title.TitleId
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
            ProfileData profile = JsonSerializer.Deserialize<ProfileData>(responseData) ?? new();
            return profile;
        }

        public async Task<ProfileData> GetMyProfile()
        {
            string apiUrl = $"https://us-prof.np.community.playstation.net/userProfile/v1/users/me/profile2?fields=accountId,onlineId,currentOnlineId";
            var responseData = await MakeRequest(apiUrl);
            ProfileData profile = JsonSerializer.Deserialize<ProfileData>(responseData) ?? new();
            return profile;
        }

        public async Task<GameData> GetTitles(int offset, string accountId)
        {
            var input = $"?limit=200&offset={offset}";
            string apiUrl = $"https://m.np.playstation.com/api/gamelist/v2/users/{accountId}/titles{input}";
            var responseData = await MakeRequest(apiUrl);
            GameData gameData = JsonSerializer.Deserialize<GameData>(responseData) ?? new();
            return gameData;

        }

        public async Task<TrophyProfileData> GetUserProfileTrophy(string accountId)
        {
            string apiUrl = $"https://m.np.playstation.com/api/trophy/v1/users/{accountId}/trophySummary";
            var responseData = await MakeRequest(apiUrl);
            TrophyProfileData trophyData = JsonSerializer.Deserialize<TrophyProfileData>(responseData) ?? new();
            return trophyData;
        }


        public async Task<TrophyData> GetUserTrophyTitles(string accountId)
        {
            string apiUrl = $"https://m.np.playstation.com/api/trophy/v1/users/{accountId}/trophyTitles";
            var responseData = await MakeRequest(apiUrl);
            TrophyData trophyData = JsonSerializer.Deserialize<TrophyData>(responseData) ?? new();
            return trophyData;
        }

        public async Task<TitleTrophyData> GetTitleTrophies(string npCommunicationId, string? npServiceName = null)
        {
            var apiUrl = $"https://m.np.playstation.com/api/trophy/v1/npCommunicationIds/{npCommunicationId}/trophyGroups/all/trophies";
            apiUrl = AppendNpServiceName(apiUrl, npServiceName);
            var responseData = await MakeRequest(apiUrl);
            return JsonSerializer.Deserialize<TitleTrophyData>(responseData) ?? new();
        }

        public async Task<UserTrophyData> GetUserTrophiesEarnedForTitle(string accountId, string npCommunicationId, string? npServiceName = null)
        {
            var apiUrl = $"https://m.np.playstation.com/api/trophy/v1/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophyGroups/all/trophies";
            apiUrl = AppendNpServiceName(apiUrl, npServiceName);
            var responseData = await MakeRequest(apiUrl);
            return JsonSerializer.Deserialize<UserTrophyData>(responseData) ?? new();
        }

        private async Task<string> MakeRequest(string apiUrl)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                using HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    return responseData;
                }

                string errorData = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PlayStation request failed. Status code: {StatusCode}. Details: {Details}", response.StatusCode, errorData);
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayStation request failed.");
                return "";
            }
        }

        private static string AppendNpServiceName(string apiUrl, string? npServiceName)
        {
            return string.IsNullOrWhiteSpace(npServiceName)
                ? apiUrl
                : $"{apiUrl}?npServiceName={Uri.EscapeDataString(npServiceName)}";
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

        public async Task ImportGames(LuminaUser user, List<MyGameDto> gamesToImport)
        {
            await _importPipeline.ImportAsync(user, gamesToImport.Select(ToImportItem));
        }

        public async Task<GameImportPreviewResult> PreviewGames(LuminaUser user, List<MyGameDto> gamesToImport)
        {
            return await _importPipeline.PreviewAsync(user, gamesToImport.Select((game, index) =>
            {
                var item = ToImportItem(game);
                item.RowNumber = index + 1;
                return item;
            }));
        }

        private static GameImportItem ToImportItem(MyGameDto gameToImport)
        {
            var game = gameToImport.Game;
            return new GameImportItem
            {
                Name = game?.Name ?? string.Empty,
                Source = string.IsNullOrWhiteSpace(game?.Source) ? "PSN" : game.Source,
                Platforms = game?.Platforms ?? 0,
                ExternalProvider = ExternalMediaProvider.Psn,
                ExternalId = game?.PsnId,
                Status = gameToImport.Status,
                Priority = 0,
                Rating = gameToImport.Rating,
                StartDate = gameToImport.StartDate,
                EndDate = gameToImport.EndDate,
                TimeSpend = gameToImport.TimeSpend,
                FirstPlayed = gameToImport.MyGameInfo?.FirstPlayed,
                LastPlayed = gameToImport.MyGameInfo?.LastPlayed,
                TrackedHours = gameToImport.MyGameInfo?.TrackedHours,
            };
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
        }
    }
}
