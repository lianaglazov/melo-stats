using Microsoft.AspNetCore.Mvc;

namespace MeloStats.Controllers
{
    using System.Threading.Tasks;
    using MeloStats.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    public class SpotifyController : Controller
    {
        private readonly SpotifyApiService _spotifyApiService;

        public SpotifyController(IConfiguration configuration)
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];

            var spotifyService = new SpotifyService(clientId, clientSecret);
            _spotifyApiService = new SpotifyApiService(spotifyService);
        }
        public async Task<IActionResult> Track(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var trackInfo = await _spotifyApiService.GetTrackAsync(id);
            return View(trackInfo);
        }
    }

}
