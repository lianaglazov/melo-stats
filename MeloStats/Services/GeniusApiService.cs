namespace MeloStats.Services
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Azure.Core;
    using MeloStats.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    public class GeniusApiService
    {
        private readonly HttpClient _httpClient;
        public GeniusApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "hT1ZzEX9FqIKK1pykNXIDITQD_Ife02p0ORH9Q_DEOGGlm17dUHS_ORK4xU806Nf");
        }

        public async Task<string> GetSongLanguageAsync(string songTitle, string artistName)
        {
            try
            {
                var songId = await GetSongIdAsync(songTitle, artistName);

                if (songId <= 0)
                {
                    return "Unknown"; 
                }

                var songUrl = $"https://api.genius.com/songs/{songId}";
                var response = await _httpClient.GetStringAsync(songUrl);
                var json = JObject.Parse(response);
                var language = json["response"]?["song"]?["language"]?.ToString();

                return language ?? "Unknown"; 
            }
            catch (HttpRequestException ex)
            {
                return "Unknown";
            }
            catch (Exception ex)
            {
                return "Unknown";
            }
        }

        public async Task<int> GetSongIdAsync(string songTitle, string artistName)
        {
            try
            {
                var searchUrl = $"https://api.genius.com/search?q={Uri.EscapeDataString(songTitle + " " + artistName)}";
                var response = await _httpClient.GetStringAsync(searchUrl);
                var json = JObject.Parse(response);
                var songId = json["response"]?["hits"]?[0]?["result"]?["id"]?.Value<int>();

                return songId ?? 0; 
            }
            catch (HttpRequestException ex)
            {
                return 0; 
            }
            catch (Exception ex)
            {
                return 0; 
            }
        }



    }
}
