using MeloStats.Data;
using MeloStats.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeloStats.Controllers
{
    public class ListeningHistoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public ListeningHistoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) 
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
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
        [HttpGet]
        public async Task<IActionResult> Weekly()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var listeningData = await _context.ListeningHistories
                .Where(l => l.PlayedAt >= sevenDaysAgo)
                .Include(l => l.Track)  
                .ToListAsync();
            var hourlyStats = listeningData
               .SelectMany(l => SplitListeningTimeByHour(l))
               .GroupBy(l => new { l.Date.DayOfWeek, l.Hour })
               .Select(g => new
               {
                   DayOfWeek = g.Key.DayOfWeek.ToString(),
                   Hour = g.Key.Hour,
                   TotalListeningTime = g.Sum(l => l.TotalListeningTime)  
               })
               .ToList();

            return View(hourlyStats);
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
