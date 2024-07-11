using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace MeloStats.Controllers
{
    using System.Threading.Tasks;
    using MeloStats.Data;
    using MeloStats.Models;
    using MeloStats.Services;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    public class SpotifyController : Controller
    {
        private readonly SpotifyApiService _spotifyApiService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;


        public SpotifyController(IConfiguration configuration, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];
            _userManager = userManager;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            var spotifyService = new SpotifyAuthService(clientId, clientSecret);
            _spotifyApiService = new SpotifyApiService(spotifyService, userManager, context, configuration, httpClientFactory);
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
