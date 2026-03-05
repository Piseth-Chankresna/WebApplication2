using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using ClosedXML.Excel;
using System.Data;

namespace WebApplication2.Controllers
{
    public class ReportController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // ==================== MAIN REPORTS PAGE ====================
        public async Task<IActionResult> Index()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfYear = new DateTime(today.Year, 1, 1);

                // Student Statistics
                ViewBag.TotalStudents = await _context.Students.CountAsync();
                ViewBag.ActiveStudents = await _context.Students.CountAsync(s => s.Status == "កំពុងសិក្សា");
                ViewBag.MaleStudents = await _context.Students.CountAsync(s => s.Gender == "ប្រុស" || s.Gender == "Male");
                ViewBag.FemaleStudents = await _context.Students.CountAsync(s => s.Gender == "ស្រី" || s.Gender == "Female");

                // Payment Statistics
                ViewBag.TotalPayments = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
                ViewBag.MonthlyPayments = await _context.Payments
                    .Where(p => p.PaymentDate >= startOfMonth)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;
                ViewBag.YearlyPayments = await _context.Payments
                    .Where(p => p.PaymentDate >= startOfYear)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                // Attendance Statistics
                var totalAttendance = await _context.Attendances.CountAsync();
                var presentAttendance = await _context.Attendances.CountAsync(a => a.Status == "P");
                ViewBag.AverageAttendance = totalAttendance > 0
                    ? (presentAttendance * 100 / totalAttendance)
                    : 0;

                // Grade Statistics
                var grades = await _context.Grades.ToListAsync();
                ViewBag.AverageGrade = grades.Count != 0 ? grades.Average(g => g.Total) : 0;
                ViewBag.PassRate = grades.Count != 0
                    ? (grades.Count(g => g.Total >= 50) * 100 / grades.Count)
                    : 0;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reports: {ex.Message}");
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return View();
            }
        }

        // ==================== API ENDPOINTS FOR CHARTS ====================

        [HttpGet]
        public async Task<JsonResult> GetStudentsByMajor()
        {
            try
            {
                var data = await _context.Students
                    .Where(s => s.Major != null)
                    .GroupBy(s => s.Major)
                    .Select(g => new
                    {
                        Major = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetStudentsByYear()
        {
            try
            {
                var data = await _context.Students
                    .GroupBy(s => s.Year)
                    .Select(g => new
                    {
                        Year = "ឆ្នាំទី" + (g.Key ?? "?"),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetStudentsByShift()
        {
            try
            {
                var data = await _context.Students
                    .Where(s => s.Shift != null)
                    .GroupBy(s => s.Shift)
                    .Select(g => new
                    {
                        Shift = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMonthlyPayments(int months = 12)
        {
            try
            {
                var startDate = DateTime.Today.AddMonths(-months + 1);
                startDate = new DateTime(startDate.Year, startDate.Month, 1);

                var payments = await _context.Payments
                    .Where(p => p.PaymentDate >= startDate)
                    .ToListAsync();

                var result = new List<object>();
                var khmerMonths = new[] { "មករា", "កុម្ភៈ", "មីនា", "មេសា", "ឧសភា", "មិថុនា",
                                          "កក្កដា", "សីហា", "កញ្ញា", "តុលា", "វិច្ឆិកា", "ធ្នូ" };

                for (int i = 0; i < months; i++)
                {
                    var month = startDate.AddMonths(i);
                    var monthPayments = payments.Where(p => p.PaymentDate.Month == month.Month
                                                         && p.PaymentDate.Year == month.Year);

                    result.Add(new
                    {
                        Month = khmerMonths[month.Month - 1],
                        month.Year,
                        Total = monthPayments.Sum(p => p.Amount),
                        Count = monthPayments.Count()
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDailyAttendance(int days = 30)
        {
            try
            {
                var startDate = DateTime.Today.AddDays(-days + 1);

                var attendance = await _context.Attendances
                    .Where(a => a.Date >= startDate)
                    .OrderBy(a => a.Date)
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Present = g.Count(x => x.Status == "P"),
                        Late = g.Count(x => x.Status == "L"),
                        Absent = g.Count(x => x.Status == "A")
                    })
                    .ToListAsync();

                return Json(new { success = true, data = attendance });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetGradeDistribution()
        {
            try
            {
                var grades = await _context.Grades.ToListAsync();

                var distribution = new
                {
                    A = grades.Count(g => g.Total >= 90),
                    B = grades.Count(g => g.Total >= 80 && g.Total < 90),
                    C = grades.Count(g => g.Total >= 70 && g.Total < 80),
                    D = grades.Count(g => g.Total >= 60 && g.Total < 70),
                    E = grades.Count(g => g.Total >= 50 && g.Total < 60),
                    F = grades.Count(g => g.Total < 50)
                };

                return Json(new { success = true, data = distribution });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTopStudents(int limit = 5)
        {
            try
            {
                var topStudents = await _context.Grades
                    .Include(g => g.Student)
                    .Where(g => g.Student != null)
                    .GroupBy(g => g.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        StudentName = g.First().Student != null ? g.First().Student.Name : "",
                        StudentPhoto = g.First().Student != null ? g.First().Student.Photo : "",
                        Major = g.First().Student != null ? g.First().Student.Major : "",
                        AverageGrade = g.Average(x => x.Total)
                    })
                    .OrderByDescending(x => x.AverageGrade)
                    .Take(limit)
                    .ToListAsync();

                return Json(new { success = true, data = topStudents });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetStrugglingStudents(int limit = 5)
        {
            try
            {
                var strugglingStudents = await _context.Grades
                    .Include(g => g.Student)
                    .Where(g => g.Student != null)
                    .GroupBy(g => g.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        StudentName = g.First().Student != null ? g.First().Student.Name : "",
                        StudentPhoto = g.First().Student != null ? g.First().Student.Photo : "",
                        Major = g.First().Student != null ? g.First().Student.Major : "",
                        AverageGrade = g.Average(x => x.Total)
                    })
                    .Where(x => x.AverageGrade < 50)
                    .OrderBy(x => x.AverageGrade)
                    .Take(limit)
                    .ToListAsync();

                return Json(new { success = true, data = strugglingStudents });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetKPI()
        {
            try
            {
                var totalStudents = await _context.Students.CountAsync();
                var totalPayments = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;

                var totalAttendance = await _context.Attendances.CountAsync();
                var presentAttendance = await _context.Attendances.CountAsync(a => a.Status == "P");
                var avgAttendance = totalAttendance > 0 ? (presentAttendance * 100 / totalAttendance) : 0;

                var grades = await _context.Grades.ToListAsync();
                var passRate = grades.Count != 0 ? (grades.Count(g => g.Total >= 50) * 100 / grades.Count) : 0;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalStudents,
                        totalPayments,
                        avgAttendance,
                        passRate,
                        studentChange = "+5%",
                        paymentChange = "+12%",
                        attendanceChange = "+3%",
                        passChange = "+2%"
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public Task<JsonResult> GetRecentActivities()
        {
            try
            {
                var activities = new List<object>
                {
                    new { action = "បន្ថែមសិស្សថ្មី", description = "បានបន្ថែមសិស្ស ៣ នាក់", timeAgo = "៥ នាទីមុន", icon = "bi-person-plus" },
                    new { action = "កត់ត្រាវត្តមាន", description = "បានកត់ត្រាវត្តមាន ២៥ នាក់", timeAgo = "១០ នាទីមុន", icon = "bi-calendar-check" },
                    new { action = "ការបង់ប្រាក់ថ្មី", description = "ទទួលបានការបង់ប្រាក់ $៥០០", timeAgo = "២០ នាទីមុន", icon = "bi-cash" },
                    new { action = "កែប្រែពិន្ទុ", description = "បានកែប្រែពិន្ទុសិស្ស ១០ នាក់", timeAgo = "៣០ នាទីមុន", icon = "bi-pencil" }
                };

                return Task.FromResult(Json(new { success = true, data = activities }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Json(new { success = false, message = ex.Message }));
            }
        }

        // ==================== EXPORT FUNCTIONS ====================

        [HttpGet]
        public async Task<IActionResult> ExportStudentReport()
        {
            try
            {
                var students = await _context.Students.ToListAsync();
                var classes = await _context.StudentClasses.ToListAsync();
                var payments = await _context.Payments.ToListAsync();

                using var wb = new XLWorkbook();

                // Student Summary Sheet
                var ws1 = wb.Worksheets.Add("Student Summary");
                ws1.Cell(1, 1).Value = "របាយការណ៍សិស្សសរុប";
                ws1.Cell(1, 1).Style.Font.Bold = true;
                ws1.Cell(1, 1).Style.Font.FontSize = 16;

                ws1.Cell(3, 1).Value = "សរុបសិស្សទាំងអស់";
                ws1.Cell(3, 2).Value = students.Count;

                ws1.Cell(4, 1).Value = "សិស្សកំពុងសិក្សា";
                ws1.Cell(4, 2).Value = students.Count(s => s.Status == "កំពុងសិក្សា");

                ws1.Cell(5, 1).Value = "សិស្សប្រុស";
                ws1.Cell(5, 2).Value = students.Count(s => s.Gender == "ប្រុស" || s.Gender == "Male");

                ws1.Cell(6, 1).Value = "សិស្សស្រី";
                ws1.Cell(6, 2).Value = students.Count(s => s.Gender == "ស្រី" || s.Gender == "Female");

                // Student List Sheet
                var ws2 = wb.Worksheets.Add("Student List");
                ws2.Cell(1, 1).Value = "ID";
                ws2.Cell(1, 2).Value = "ឈ្មោះ";
                ws2.Cell(1, 3).Value = "ភេទ";
                ws2.Cell(1, 4).Value = "ជំនាញ";
                ws2.Cell(1, 5).Value = "ឆ្នាំ";
                ws2.Cell(1, 6).Value = "វេន";
                ws2.Cell(1, 7).Value = "ស្ថានភាព";

                int row = 2;
                foreach (var s in students)
                {
                    ws2.Cell(row, 1).Value = s.Id;
                    ws2.Cell(row, 2).Value = s.Name;
                    ws2.Cell(row, 3).Value = s.Gender;
                    ws2.Cell(row, 4).Value = s.Major;
                    ws2.Cell(row, 5).Value = "ឆ្នាំទី" + s.Year;
                    ws2.Cell(row, 6).Value = s.Shift;
                    ws2.Cell(row, 7).Value = s.Status ?? "កំពុងសិក្សា";
                    row++;
                }

                ws2.RangeUsed().SetAutoFilter();
                ws2.Columns().AdjustToContents();

                // Payment Summary Sheet
                if (payments.Count != 0)
                {
                    var ws3 = wb.Worksheets.Add("Payment Summary");
                    ws3.Cell(1, 1).Value = "កាលបរិច្ឆេទ";
                    ws3.Cell(1, 2).Value = "សិស្ស";
                    ws3.Cell(1, 3).Value = "វិក្កយបត្រ";
                    ws3.Cell(1, 4).Value = "ទឹកប្រាក់";

                    row = 2;
                    foreach (var p in payments.OrderByDescending(p => p.PaymentDate))
                    {
                        ws3.Cell(row, 1).Value = p.PaymentDate.ToString("dd/MM/yyyy");
                        ws3.Cell(row, 2).Value = p.StudentName;
                        ws3.Cell(row, 3).Value = p.ReceiptNumber;
                        ws3.Cell(row, 4).Value = p.Amount;
                        row++;
                    }

                    ws3.Columns().AdjustToContents();
                }

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Student_Report_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហាក្នុងការនាំចេញរបាយការណ៍: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPaymentReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddMonths(-1);
                var end = endDate ?? DateTime.Today;

                var payments = await _context.Payments
                    .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Payment Report");

                ws.Cell(1, 1).Value = "របាយការណ៍ការបង់ប្រាក់";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;

                ws.Cell(3, 1).Value = "ចាប់ពីថ្ងៃទី:";
                ws.Cell(3, 2).Value = start.ToString("dd/MM/yyyy");
                ws.Cell(4, 1).Value = "ដល់ថ្ងៃទី:";
                ws.Cell(4, 2).Value = end.ToString("dd/MM/yyyy");

                ws.Cell(6, 1).Value = "វិក្កយបត្រ";
                ws.Cell(6, 2).Value = "កាលបរិច្ឆេទ";
                ws.Cell(6, 3).Value = "សិស្ស";
                ws.Cell(6, 4).Value = "ទឹកប្រាក់";
                ws.Cell(6, 5).Value = "វិធីបង់";
                ws.Cell(6, 6).Value = "ឆមាស";

                int row = 7;
                decimal total = 0;
                foreach (var p in payments)
                {
                    ws.Cell(row, 1).Value = p.ReceiptNumber;
                    ws.Cell(row, 2).Value = p.PaymentDate.ToString("dd/MM/yyyy");
                    ws.Cell(row, 3).Value = p.StudentName;
                    ws.Cell(row, 4).Value = p.Amount;
                    ws.Cell(row, 5).Value = p.PaymentMethod == "cash" ? "សាច់ប្រាក់" : p.PaymentMethod == "aba" ? "ABA Pay" : "Wing";
                    ws.Cell(row, 6).Value = p.Semester;

                    total += p.Amount;
                    row++;
                }

                ws.Cell(row, 3).Value = "សរុប:";
                ws.Cell(row, 4).Value = total;
                ws.Cell(row, 4).Style.Font.Bold = true;

                ws.Columns().AdjustToContents();
                ws.RangeUsed().SetAutoFilter();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Payment_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហាក្នុងការនាំចេញរបាយការណ៍: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendanceReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddMonths(-1);
                var end = endDate ?? DateTime.Today;

                var attendances = await _context.Attendances
                    .Where(a => a.Date >= start && a.Date <= end)
                    .OrderBy(a => a.Date)
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Attendance Report");

                ws.Cell(1, 1).Value = "របាយការណ៍វត្តមាន";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;

                ws.Cell(3, 1).Value = "ចាប់ពីថ្ងៃទី:";
                ws.Cell(3, 2).Value = start.ToString("dd/MM/yyyy");
                ws.Cell(4, 1).Value = "ដល់ថ្ងៃទី:";
                ws.Cell(4, 2).Value = end.ToString("dd/MM/yyyy");

                ws.Cell(6, 1).Value = "កាលបរិច្ឆេទ";
                ws.Cell(6, 2).Value = "សិស្ស";
                ws.Cell(6, 3).Value = "ស្ថានភាព";
                ws.Cell(6, 4).Value = "កំណត់ចំណាំ";

                int row = 7;
                foreach (var a in attendances)
                {
                    ws.Cell(row, 1).Value = a.Date.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = a.StudentName;

                    string status = a.Status == "P" ? "វត្តមាន" : a.Status == "L" ? "មកយឺត" : "អវត្តមាន";
                    ws.Cell(row, 3).Value = status;
                    ws.Cell(row, 4).Value = a.Note ?? "";

                    row++;
                }

                ws.Columns().AdjustToContents();
                ws.RangeUsed().SetAutoFilter();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Attendance_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហាក្នុងការនាំចេញរបាយការណ៍: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportGradeReport()
        {
            try
            {
                var grades = await _context.Grades
                    .Include(g => g.Student)
                    .OrderByDescending(g => g.Total)
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Grade Report");

                ws.Cell(1, 1).Value = "របាយការណ៍ពិន្ទុសិស្ស";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;

                ws.Cell(3, 1).Value = "ល.រ";
                ws.Cell(3, 2).Value = "សិស្ស";
                ws.Cell(3, 3).Value = "ជំនាញ";
                ws.Cell(3, 4).Value = "មុខវិជ្ជា";
                ws.Cell(3, 5).Value = "វត្តមាន";
                ws.Cell(3, 6).Value = "កិច្ចការ";
                ws.Cell(3, 7).Value = "ពាក់កណ្តាលឆ្នាំ";
                ws.Cell(3, 8).Value = "ប្រឡងចុងឆ្នាំ";
                ws.Cell(3, 9).Value = "ពិន្ទុសរុប";
                ws.Cell(3, 10).Value = "និទ្ទេស";

                int row = 4;
                int index = 1;
                foreach (var g in grades)
                {
                    ws.Cell(row, 1).Value = index++;
                    ws.Cell(row, 2).Value = g.StudentName ?? g.Student?.Name ?? "N/A";
                    ws.Cell(row, 3).Value = g.Student?.Major ?? "N/A";
                    ws.Cell(row, 4).Value = g.Subject ?? "ទូទៅ";
                    ws.Cell(row, 5).Value = g.Attendance;
                    ws.Cell(row, 6).Value = g.Assignment;
                    ws.Cell(row, 7).Value = g.MidTerm;
                    ws.Cell(row, 8).Value = g.FinalExam;
                    ws.Cell(row, 9).Value = g.Total;
                    ws.Cell(row, 10).Value = g.GradeLetter ?? "F";

                    row++;
                }

                ws.Columns().AdjustToContents();
                ws.RangeUsed().SetAutoFilter();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Grade_Report_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហាក្នុងការនាំចេញរបាយការណ៍: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}