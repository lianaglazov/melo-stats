using Microsoft.AspNetCore.Mvc;
using MeloStats.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MeloStats.Models;

namespace MeloStats.Controllers
{
    public class TracksController : Controller
    {
        private readonly SpotifyApiService _spotifyApiService;
        private readonly UserManager<ApplicationUser> _userManager;
        public TracksController(SpotifyApiService spotifyApiService, UserManager<ApplicationUser> userManager)
        {
            _spotifyApiService = spotifyApiService;
            _userManager = userManager;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TopTracks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var topTracks = await _spotifyApiService.GetTopTracksAsync(user);

            // Pass topTracks to the view or return as needed
            return View(topTracks);
        }
    }
}
