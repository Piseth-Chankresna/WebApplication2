using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using System.Diagnostics;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class HomeController(ILogger<HomeController> logger, ApplicationDbContext context, ActivityLogService activityLogService) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLogService = activityLogService;

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

                // ស្ថិតិទូទៅ
                var totalStudents = await _context.Students.CountAsync();
                var totalTeachers = await _context.StudentClasses
                    .Select(c => c.TeacherName)
                    .Where(t => t != null)
                    .Distinct()
                    .CountAsync();
                var totalClasses = await _context.StudentClasses.CountAsync();
                var totalPayments = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;

                // វត្តមានថ្ងៃនេះ
                var todayAttendanceData = await _context.Attendances
                    .Where(a => a.Date == today)
                    .GroupBy(a => a.Status ?? "")
                    .Select(g => new { Status = g.Key ?? "", Count = g.Count() })
                    .ToListAsync();

                var todayAttendance = new Dictionary<string, int>();
                foreach (var item in todayAttendanceData)
                {
                    todayAttendance[item.Status] = item.Count;
                }

                // បង់ប្រាក់ប្រចាំខែ
                var monthlyPayment = await _context.Payments
                    .Where(p => p.PaymentDate >= startOfMonth)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                // សកម្មភាពថ្មីៗ
                var recentActivities = await _context.ActivityLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();

                // សិស្សពូកែ (Top 5 GPA)
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var topStudents = await _context.Grades
                    .Include(g => g.Student)
                    .Where(g => g.Student != null)
                    .GroupBy(g => g.StudentId ?? "")
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        StudentName = g.First().Student != null ? g.First().Student.Name : "",
                        StudentPhoto = g.First().Student != null ? g.First().Student.Photo : "",
                        StudentMajor = g.First().Student != null ? g.First().Student.Major : "",
                        AvgGrade = g.Average(x => (double?)x.Total) ?? 0
                    })
                    .OrderByDescending(x => x.AvgGrade)
                    .Take(5)
                    .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // សិស្សដែលត្រូវការជំនួយ (Low GPA)
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var strugglingStudents = await _context.Grades
                    .Include(g => g.Student)
                    .Where(g => g.Student != null)
                    .GroupBy(g => g.StudentId ?? "")
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        StudentName = g.First().Student != null ? g.First().Student.Name : "",
                        StudentPhoto = g.First().Student != null ? g.First().Student.Photo : "",
                        StudentMajor = g.First().Student != null ? g.First().Student.Major : "",
                        AvgGrade = g.Average(x => (double?)x.Total) ?? 0
                    })
                    .Where(x => x.AvgGrade < 50)
                    .OrderBy(x => x.AvgGrade)
                    .Take(5)
                    .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // កាលបរិច្ឆេទសំខាន់ៗ
                var upcomingEvents = new List<DashboardEvent>
                {
                    new() {
                        Date = today.AddDays(3),
                        Title = "ថ្ងៃផុតកំណត់បង់ប្រាក់",
                        Type = "warning",
                        Description = "សិស្សដែលមិនទាន់បង់ប្រាក់ត្រូវបង់មុនថ្ងៃនេះ"
                    },
                    new() {
                        Date = today.AddDays(7),
                        Title = "ប្រឡងពាក់កណ្តាលឆ្នាំ",
                        Type = "important",
                        Description = "ប្រឡងឆមាសទី១ ឆ្នាំសិក្សា 2025-2026"
                    },
                    new() {
                        Date = today.AddDays(14),
                        Title = "កិច្ចប្រជុំគណៈគ្រប់គ្រង",
                        Type = "info",
                        Description = "ប្រជុំពិភាក្សាពីការអភិវឌ្ឍន៍កម្មវិធីសិក្សា"
                    },
                    new() {
                        Date = today.AddDays(21),
                        Title = "ទិវាជាតិ",
                        Type = "holiday",
                        Description = "ច្បាប់ឈប់សម្រាក"
                    },
                    new() {
                        Date = today.AddDays(30),
                        Title = "ប្រកាសពិន្ទុប្រឡង",
                        Type = "success",
                        Description = "ប្រកាសលទ្ធផលប្រឡងពាក់កណ្តាលឆ្នាំ"
                    }
                };

                // ស្ថិតិតាមជំនាញ
                var studentsByMajor = await _context.Students
                    .Where(s => s.Major != null)
                    .GroupBy(s => s.Major ?? "")
                    .Select(g => new { Major = g.Key, Count = g.Count() })
                    .ToListAsync();

                // ស្ថិតិតាមឆ្នាំ
                var studentsByYear = await _context.Students
                    .GroupBy(s => s.Year ?? "")
                    .Select(g => new { Year = g.Key, Count = g.Count() })
                    .ToListAsync();

                // ស្ថិតិវត្តមានប្រចាំសប្តាហ៍
                var weeklyAttendance = await _context.Attendances
                    .Where(a => a.Date >= startOfWeek && a.Date <= today)
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Present = g.Count(x => x.Status == "P"),
                        Late = g.Count(x => x.Status == "L"),
                        Absent = g.Count(x => x.Status == "A")
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // ស្ថិតិការបង់ប្រាក់ប្រចាំខែ (6 ខែចុងក្រោយ)
                var sixMonthsAgo = today.AddMonths(-6);
                var monthlyPayments = await _context.Payments
                    .Where(p => p.PaymentDate >= sixMonthsAgo)
                    .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(p => p.Amount),
                        Count = g.Count()
                    })
                    .OrderBy(g => g.Year)
                    .ThenBy(g => g.Month)
                    .ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    TotalStudents = totalStudents,
                    TotalTeachers = totalTeachers,
                    TotalClasses = totalClasses,
                    TotalPayments = totalPayments,
                    TodayAttendance = todayAttendance,
                    MonthlyPayment = monthlyPayment,
                    RecentActivities = recentActivities,
                    TopStudents = [.. topStudents.Select(s => new TopStudentViewModel
                    {
                        Id = s.StudentId,
                        Name = s.StudentName ?? "",
                        Photo = s.StudentPhoto,
                        Major = s.StudentMajor ?? "",
                        AverageGrade = s.AvgGrade
                    })],
                    StrugglingStudents = [.. strugglingStudents.Select(s => new StrugglingStudentViewModel
                    {
                        Id = s.StudentId,
                        Name = s.StudentName ?? "",
                        Photo = s.StudentPhoto,
                        Major = s.StudentMajor ?? "",
                        AverageGrade = s.AvgGrade
                    })],
                    UpcomingEvents = upcomingEvents,
                    StudentsByMajor = [.. studentsByMajor.Cast<object>()],
                    StudentsByYear = [.. studentsByYear.Cast<object>()],
                    WeeklyAttendance = [.. weeklyAttendance.Cast<object>()],
                    MonthlyPayments = [.. monthlyPayments.Cast<object>()]
                };

                // កត់ត្រាសកម្មភាព
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "មើល Dashboard",
                    $"បានចូលមើលទំព័រ Dashboard"
                );

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទិន្នន័យ Dashboard";
                return View(new DashboardViewModel());
            }
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

        public IActionResult AccessDenied()
        {
            return View();
        }

        // API endpoints for dashboard widgets
        [HttpGet]
        public async Task<JsonResult> GetAttendanceStats()
        {
            var today = DateTime.Today;
            var stats = await _context.Attendances
                .Where(a => a.Date == today)
                .GroupBy(a => a.Status ?? "")
                .Select(g => new { Status = g.Key ?? "", Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            return Json(new
            {
                present = stats.GetValueOrDefault("P", 0),
                late = stats.GetValueOrDefault("L", 0),
                absent = stats.GetValueOrDefault("A", 0)
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetWeeklyAttendance()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var stats = await _context.Attendances
                .Where(a => a.Date >= startOfWeek && a.Date <= today)
                .GroupBy(a => a.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Present = g.Count(x => x.Status == "P"),
                    Late = g.Count(x => x.Status == "L"),
                    Absent = g.Count(x => x.Status == "A")
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Json(stats);
        }

        [HttpGet]
        public async Task<JsonResult> GetPaymentStats()
        {
            var today = DateTime.Today;
            var sixMonthsAgo = today.AddMonths(-6);

            var stats = await _context.Payments
                .Where(p => p.PaymentDate >= sixMonthsAgo)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            return Json(stats);
        }

        [HttpGet]
        public async Task<JsonResult> GetRecentActivities()
        {
            var activities = await _context.ActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new
                {
                    a.UserName,
                    a.Action,
                    a.Description,
                    a.Timestamp,
                    TimeAgo = GetTimeAgo(a.Timestamp)
                })
                .ToListAsync();

            return Json(activities);
        }

        [HttpGet]
        public async Task<JsonResult> GetTopStudents()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var topStudents = await _context.Grades
                .Include(g => g.Student)
                .Where(g => g.Student != null)
                .GroupBy(g => g.StudentId ?? "")
                .Select(g => new
                {
                    StudentId = g.Key,
                    StudentName = g.First().Student != null ? g.First().Student.Name : "",
                    StudentPhoto = g.First().Student != null ? g.First().Student.Photo : "",
                    StudentMajor = g.First().Student != null ? g.First().Student.Major : "",
                    AvgGrade = g.Average(x => (double?)x.Total) ?? 0
                })
                .OrderByDescending(x => x.AvgGrade)
                .Take(5)
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return Json(topStudents);
        }

        private static string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "ទើបតែប៉ុន្មានវិនាទីមុន";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} នាទីមុន";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} ម៉ោងមុន";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ថ្ងៃមុន";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} សប្តាហ៍មុន";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} ខែមុន";
            return $"{(int)(timeSpan.TotalDays / 365)} ឆ្នាំមុន";
        }
    }

    // View Models
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public decimal TotalPayments { get; set; }
        public Dictionary<string, int> TodayAttendance { get; set; } = [];
        public decimal MonthlyPayment { get; set; }
        public List<ActivityLog> RecentActivities { get; set; } = [];
        public List<TopStudentViewModel> TopStudents { get; set; } = [];
        public List<StrugglingStudentViewModel> StrugglingStudents { get; set; } = [];
        public List<DashboardEvent> UpcomingEvents { get; set; } = [];
        public List<object> StudentsByMajor { get; set; } = [];
        public List<object> StudentsByYear { get; set; } = [];
        public List<object> WeeklyAttendance { get; set; } = [];
        public List<object> MonthlyPayments { get; set; } = [];
    }

    public class TopStudentViewModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Photo { get; set; }
        public string Major { get; set; } = "";
        public double AverageGrade { get; set; }
    }

    public class StrugglingStudentViewModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Photo { get; set; }
        public string Major { get; set; } = "";
        public double AverageGrade { get; set; }
    }

    public class DashboardEvent
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }
}