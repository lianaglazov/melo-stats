using MeloStats.Data;
using MeloStats.Models;
using MeloStats.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MeloStats.Controllers
{
    public class ListeningHistoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SpotifyApiService _spotifyApiService;
        private readonly GeniusApiService _geniusApiService;
        public ListeningHistoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SpotifyApiService spotifyApiService, GeniusApiService geniusApiService) 
        {
            _context = context;
            _userManager = userManager;
            _spotifyApiService = spotifyApiService;
            _geniusApiService = geniusApiService;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Hours()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }
            var listened = await _context.ListeningHistories.Where(l => l.UserId == user.Id)
                            .Include(l => l.Track)
                            .GroupBy(l=> l.PlayedAt.Hour)
                            .Select(g => new 
                            {
                                ListeningHour = g.Key,
                                TotalListeningTime = g.Sum(l => l.Track.Duration)  
                            })
                            .OrderBy(l => l.ListeningHour).ToListAsync();
            return View(listened);

        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Days()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }
            var listeningHistory = await _context.ListeningHistories
                            .Where(l => l.UserId == user.Id)
                            .Include(l => l.Track) 
                            .ToListAsync(); 

            var listened = listeningHistory
                .GroupBy(l => l.PlayedAt.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    TotalListeningTime = g.Sum(l => l.Track.Duration) 
                })
                .OrderBy(x => x.DayOfWeek)
                .ToList();
            return View(listened);

        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Weekly()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var listeningData = await _context.ListeningHistories
                .Where(l => l.PlayedAt >= sevenDaysAgo)
                .Include(l => l.Track)
                .ToListAsync();
            var hourlyStats = new Dictionary<DateTime, Dictionary<int, double>>();

            foreach (var listening in listeningData)
            {
                var hourlySegments = SplitListeningTimeByHour(listening);
                foreach (var segment in hourlySegments)
                {
                    if (!hourlyStats.ContainsKey(segment.Date))
                    {
                        hourlyStats[segment.Date] = new Dictionary<int, double>();
                    }

                    if (!hourlyStats[segment.Date].ContainsKey(segment.Hour))
                    {
                        hourlyStats[segment.Date][segment.Hour] = 0;
                    }

                    hourlyStats[segment.Date][segment.Hour] += segment.TotalListeningTime;
                }
            }
            hourlyStats = hourlyStats.OrderBy(ob => ob.Key).ToDictionary(obj => obj.Key, obj => obj.Value);
            var allDates = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).Date)
                .ToList();
            ViewBag.AllDates = allDates;

            return View(hourlyStats);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Users");
            }
            var meanPopularity = await _context.ListeningHistories
            .Where(lh => lh.UserId == user.Id)
            .Join(
                _context.Tracks,
                lh => lh.TrackId,
                t => t.Id,
                (lh, t) => t.Popularity 
            )
            .AverageAsync(p => p);
            ViewBag.MeanPopularity = meanPopularity;

            var tracks = await _context.ListeningHistories
                .Where(l => l.UserId == user.Id)
                .Include(l => l.Track)
                .Select(l => l.Track).ToListAsync();

            var Danceability = 0.0;
            var Energy = 0.0;
            var Tempo = 0.0;
            var Valence = 0.0;
            var Instrumentalness = 0.0;
            var count = tracks.Count();
           
            List<string> languages = new List<string>();
            foreach (var track in tracks)
            {
                var features = await _context.Features
                .FirstOrDefaultAsync(f => f.TrackId == track.Id);

                if(features == null)
                {
                    features = await _spotifyApiService.GetTrackFeaturesAsync(track.Id);
                }
                track.FeatureId = features.Id;
                _context.Tracks.Update(track);
                _context.SaveChanges();
                Danceability += features.Danceability;
                Energy += features.Energy;
                Tempo += features.Tempo;
                Valence += features.Valence;
                Instrumentalness += features.Instrumentalness;
            }
            ViewBag.Danceability = Danceability / count;
            ViewBag.Energy = Energy / count;
            ViewBag.Tempo = Tempo / count;
            ViewBag.Valence = Valence / count;
            ViewBag.Instrumentalness = Instrumentalness / count;
            return View("Stats");
        }

        private IEnumerable<(DateTime Date, int Hour, double TotalListeningTime)> SplitListeningTimeByHour(ListeningHistory l)
        {
            var start = l.PlayedAt;
            var end = l.PlayedAt.AddSeconds(l.Track.Duration); 
            var listeningTime = (double)l.Track.Duration;

            var results = new List<(DateTime Date, int Hour, double TotalListeningTime)>();

            for (var time = start; time < end; time = time.AddHours(1))
            {
                var nextHour = time.AddHours(1);
                var segmentStart = time;
                var segmentEnd = nextHour > end ? end : nextHour;
                var segmentDuration = (segmentEnd - segmentStart).TotalSeconds;
                var segmentListeningTime = Math.Min(listeningTime, segmentDuration);

                results.Add((Date: segmentStart.Date, Hour: segmentStart.Hour, TotalListeningTime: segmentListeningTime));

                listeningTime -= segmentListeningTime;
                if (listeningTime <= 0)
                    break;
            }

            return results;
        }

    }
    
}
