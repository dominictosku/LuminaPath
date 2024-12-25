using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TestPSN.Classes;
using static TestPSN.Classes.PSNProfile;
using static TestPSN.Classes.PSNTitles;
using static TestPSN.Classes.PSNTrophy;

namespace TestPSN
{
    public class PSNService
    {
        string bearerToken = string.Empty;

        public void SetBearer(string token) => bearerToken = token;
        public async Task<ProfileData> GetProfile(string userName)
        {
            string apiUrl = $"https://us-prof.np.community.playstation.net/userProfile/v1/users/{userName}/profile2?fields=accountId,onlineId,currentOnlineId";
            var responseData = await MakeRequest(apiUrl);
            ProfileData profile = JsonSerializer.Deserialize<PSNProfile.ProfileData>(responseData);
            return profile;
        }

        public async Task<ProfileData> GetMyProfile()
        {
            string apiUrl = $"https://us-prof.np.community.playstation.net/userProfile/v1/users/me/profile2?fields=accountId,onlineId,currentOnlineId";
            var responseData = await MakeRequest(apiUrl);
            ProfileData profile = JsonSerializer.Deserialize<PSNProfile.ProfileData>(responseData);
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
