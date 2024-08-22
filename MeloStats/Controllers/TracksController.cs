using Microsoft.AspNetCore.Mvc;
using MeloStats.Services;
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
        public async Task<IActionResult> TopTracks(string timeRange = "medium_term")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var topTracks = await _spotifyApiService.GetTopTracksAsync(user, timeRange);

            return View(topTracks);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RecentTracks()
        {

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var recentTracks = await _spotifyApiService.GetRecentlyPlayedTracksAsync(user);

            return View(recentTracks);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TrackFeatures(int trackId)
        {
            
            var features = await _spotifyApiService.GetTrackFeaturesAsync(trackId);
            if (features == null)
            {
                return NotFound(); 
            }
            return View(features); // to be modified, it should not return the view
        }


    }
}
