using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

namespace WebApplication2.Controllers
{
    public class ActivityLogController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            var logs = await _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> ClearLogs()
        {
            var logs = await _context.ActivityLogs.ToListAsync();
            _context.ActivityLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}