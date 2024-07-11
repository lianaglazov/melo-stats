namespace MeloStats.Services
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using MeloStats.Controllers;
    using Newtonsoft.Json.Linq;
    public class SpotifyAuthService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private static readonly string TokenUrl = "https://accounts.spotify.com/api/token";


        public SpotifyAuthService(string clientId, string clientSecret)
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

        public async Task<bool> SaveUserTokensAsync(string userId, string accessToken, string refreshToken, string tokenType, int expiresIn)
        {
            try
            {
                // Construct the request to store tokens (example for Spotify)
                var tokenStoreUrl = "https://api.yourtokenstore.com/store"; // Replace with your token storage endpoint
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, tokenStoreUrl);

                    // Add headers if required
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "your_access_token_here");

                    // Create the JSON payload for the request body
                    var payload = new JObject
            {
                { "userId", userId },
                { "accessToken", accessToken },
                { "refreshToken", refreshToken },
                { "tokenType", tokenType },
                { "expiresIn", expiresIn }
                // Add other fields as needed
            };

                    // Convert the payload to string content
                    request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                    // Send the request and process the response
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode(); // Ensure successful response (200-299)

                    // Optionally handle response content if needed
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = JObject.Parse(responseBody);

                    // Example: Log success or handle response as necessary
                    //_logger.LogInformation("User tokens saved successfully for user: {UserId}", userId);

                    return true; // Indicate success
                }
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Error occurred while saving user tokens: {Message}", ex.Message);
                return false; // Indicate failure
            }
        }


    }
}
