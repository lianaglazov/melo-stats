namespace MeloStats.Services
{
    using MeloStats.Data;
    using MeloStats.Models;
    using Microsoft.AspNetCore.Identity;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System.Text;
    using Azure.Core;

    public class SpotifyApiService
    {
        private static readonly string BaseUrl = "https://api.spotify.com/v1/";
        private readonly SpotifyAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SpotifyApiService(SpotifyAuthService authService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TrackInfo> GetTrackAsync(string trackId)
        {
            var accessToken = await _authService.GetAccessTokenAsync();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync($"tracks/{trackId}");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);

                var trackInfo = new TrackInfo
                {
                    Name = json["name"].ToString(),
                    Artist = json["artists"][0]["name"].ToString(),
                    Album = json["album"]["name"].ToString()
                };

                return trackInfo;
            }
        }

        private async Task<JObject> FetchWebApi(ApplicationUser user, string endpoint, HttpMethod method, object body = null)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method, $"https://api.spotify.com/{endpoint}");

            var userToken = await _context.SpotifyTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

            var accessToken = await RefreshAccessToken(userToken.RefreshToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = client.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error fetching data from Spotify API: {response.StatusCode}, {errorContent}");
            }
            //_context.SaveChanges();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseContent);
        }

        public async Task<List<TrackInfo>> GetTopTracksAsync(ApplicationUser user, int limit = 10, string timeRange = "medium_term")
        {
            var endpoint = string.Format("v1/me/top/tracks?time_range={0}&limit={1}", timeRange, limit);
            var response = await FetchWebApi(user, endpoint, HttpMethod.Get);
            var items = response["items"];
            var topTracks = new List<TrackInfo>();

            foreach (var item in items)
            {
                var track = new TrackInfo
                {
                    Name = item["name"].ToString(),
                    Artist = string.Join(", ", item["artists"].Select(a => a["name"].ToString())),
                    Album = item["album"]["name"].ToString()
                };
                topTracks.Add(track);
            }

            return topTracks;
        }
        
        private async Task<string> RefreshAccessToken(string refreshToken)
        {
            try
            {
                var tokenEndpoint = "https://accounts.spotify.com/api/token";

                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                using (var client = new HttpClient())
                {
                    var requestBody = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret)
                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                    {
                        Content = new FormUrlEncodedContent(requestBody)
                    };

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(responseContent);

                        // Extract the access token from the JSON response
                        var accessToken = jsonResponse["access_token"].ToString();
                        var userToken = await _context.SpotifyTokens.FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
                        if (userToken != null)
                        {
                            userToken.AccessToken = accessToken;
                            userToken.CreatedAt = DateTime.UtcNow;
                            _context.SpotifyTokens.Update(userToken);
                            _context.SaveChanges();
                        }

                        return accessToken;
                    }
                    else
                    {
                        throw new Exception($"Failed to refresh access token. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here, log them, or rethrow as needed
                throw new Exception("Error refreshing access token", ex);
            }
        }


    }



    public class TrackInfo
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
    }

}
