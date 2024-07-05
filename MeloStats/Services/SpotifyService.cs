namespace MeloStats.Services
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    public class SpotifyService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private static readonly string TokenUrl = "https://accounts.spotify.com/api/token";

        public SpotifyService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }
        public async Task<string> GetAccessTokenAsync()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
                var clientCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);
                return json["access_token"].ToString();
            }
        }

    }
}
