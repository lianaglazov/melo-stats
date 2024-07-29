using MeloStats.Models;
using MeloStats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeloStats.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly SpotifyApiService _spotifyApiService;
        private readonly UserManager<ApplicationUser> _userManager;
        public ArtistsController(SpotifyApiService spotifyApiService, UserManager<ApplicationUser> userManager)
        {
            _spotifyApiService = spotifyApiService;
            _userManager = userManager;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TopArtists(string timeRange = "medium_term")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var topArtists = await _spotifyApiService.GetTopArtistsAsync(user, timeRange);

            return View(topArtists);
        }
    }
}
