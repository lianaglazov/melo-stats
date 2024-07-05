namespace MeloStats.Services
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class SpotifyApiService
    {
        private static readonly string BaseUrl = "https://api.spotify.com/v1/";
        private readonly SpotifyService _spotifyService;

        public SpotifyApiService(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        public async Task<TrackInfo> GetTrackAsync(string trackId)
        {
            var accessToken = await _spotifyService.GetAccessTokenAsync();

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
    }
    public class TrackInfo
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
    }

}
