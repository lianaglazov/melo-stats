using Microsoft.AspNetCore.Mvc;

namespace MeloStats.Controllers
{
    public class PlaylistsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
