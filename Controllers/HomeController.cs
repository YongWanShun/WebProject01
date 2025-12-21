using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebProject.Models;
using Microsoft.EntityFrameworkCore;

namespace WebProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WebProjectContext _context;
        public HomeController(ILogger<HomeController> logger, WebProjectContext context)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                                      .Include(p => p.User)
                                      .Include(p => p.Category)
                                      .OrderByDescending(p => p.CreatedAt)
                                      .ToListAsync();
            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
