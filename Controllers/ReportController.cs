using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

namespace WebApplication2.Controllers
{
    public class ReportController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            // ស្ថិតិទូទៅ
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalClasses = await _context.StudentClasses.CountAsync();
            ViewBag.TotalPayments = await _context.Payments.SumAsync(p => p.Amount);
            ViewBag.AvgAttendance = await CalculateAvgAttendance();

            // ទិន្នន័យសម្រាប់ក្រាហ្វ
            ViewBag.StudentsByMajor = await GetStudentsByMajor();
            ViewBag.StudentsByYear = await GetStudentsByYear();
            ViewBag.MonthlyPayments = await GetMonthlyPayments();

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetAttendanceTrends()
        {
            var trends = await _context.Attendances
                .GroupBy(a => a.Date)
                .Select(g => new {
                    Date = g.Key,
                    Present = g.Count(a => a.Status == "P"),
                    Late = g.Count(a => a.Status == "L"),
                    Absent = g.Count(a => a.Status == "A")
                })
                .OrderBy(g => g.Date)
                .Take(30)
                .ToListAsync();

            return Json(trends);
        }

        [HttpGet]
        public async Task<JsonResult> GetPaymentSummary()
        {
            var summary = await _context.Payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
                .Take(12)
                .ToListAsync();

            return Json(summary);
        }

        private async Task<decimal> CalculateAvgAttendance()
        {
            var total = await _context.Attendances.CountAsync();
            if (total == 0) return 0;

            var present = await _context.Attendances.CountAsync(a => a.Status == "P");
            return (decimal)present / total * 100;
        }

        private async Task<object> GetStudentsByMajor()
        {
            return await _context.Students
                .GroupBy(s => s.Major)
                .Select(g => new { Major = g.Key, Count = g.Count() })
                .ToListAsync();
        }

        private async Task<object> GetStudentsByYear()
        {
            return await _context.Students
                .GroupBy(s => s.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .ToListAsync();
        }

        private async Task<object> GetMonthlyPayments()
        {
            return await _context.Payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new {
                    g.Key.Month,
                    g.Key.Year,
                    Total = g.Sum(p => p.Amount)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .Take(12)
                .ToListAsync();
        }
    }
}