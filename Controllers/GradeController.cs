using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class GradeController(ApplicationDbContext context, ActivityLogService activityLogService, ILogger<GradeController> logger) : BaseController
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLogService = activityLogService;
        private readonly ILogger<GradeController> _logger = logger;

        // ==================== VIEWS ====================

        public IActionResult Index()
        {
            if (!PermissionService.HasPermission(User, "Grade", "View"))
                return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public IActionResult Create()
        {
            if (!PermissionService.HasPermission(User, "Grade", "Create"))
                return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (!PermissionService.HasPermission(User, "Grade", "Edit"))
                return RedirectToAction("AccessDenied", "Account");
                
            try
            {
                var grade = await _context.Grades
                    .Include(g => g.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);
                
                if (grade == null)
                {
                    return NotFound();
                }
                return View(grade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit page for grade {GradeId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការក្សាទុក";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!PermissionService.HasPermission(User, "Grade", "View"))
                return RedirectToAction("AccessDenied", "Account");
                
            try
            {
                var grade = await _context.Grades
                    .Include(g => g.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);
                
                if (grade == null)
                {
                    return NotFound();
                }
                return View(grade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Details page for grade {GradeId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការក្សាទុក";
                return RedirectToAction("Index");
            }
        }

        // ==================== API ENDPOINTS ====================

        [HttpGet]
        public async Task<IActionResult> GetGrades()
        {
            try
            {
                var grades = await _context.Grades
                    .Include(g => g.Student)
                    .OrderByDescending(g => g.RecordedAt)
                    .ToListAsync();

                var result = grades.Select(g => new
                {
                    id = g.Id,
                    studentId = g.StudentId,
                    studentName = g.Student != null ? (g.Student.FullNameKhmer ?? g.Student.FullName ?? "Unknown") : "Unknown",
                    subject = g.Subject,
                    attendance = g.Attendance,
                    assignment = g.Assignment,
                    midTerm = g.MidTerm,
                    finalExam = g.FinalExam,
                    total = g.Total,
                    gradeLetter = g.GradeLetter,
                    semester = g.Semester,
                    academicYear = g.AcademicYear,
                    recordedAt = g.RecordedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grades");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGrade(int id)
        {
            try
            {
                var grade = await _context.Grades
                    .Include(g => g.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (grade == null)
                {
                    return ErrorResponse("Grade not found", 404);
                }

                var result = new
                {
                    id = grade.Id,
                    studentId = grade.StudentId,
                    studentName = grade.Student != null ? (grade.Student.FullNameKhmer ?? grade.Student.FullName ?? "Unknown") : "Unknown",
                    subject = grade.Subject,
                    attendance = grade.Attendance,
                    assignment = grade.Assignment,
                    midTerm = grade.MidTerm,
                    finalExam = grade.FinalExam,
                    total = grade.Total,
                    gradeLetter = grade.GradeLetter,
                    semester = grade.Semester,
                    academicYear = grade.AcademicYear,
                    recordedAt = grade.RecordedAt.ToString("yyyy-MM-dd HH:mm")
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grade {GradeId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Grade grade)
        {
            try
            {
                if (!PermissionService.HasPermission(User, "Grade", "Create"))
                    return RedirectToAction("AccessDenied", "Account");

                if (grade == null)
                {
                    return ErrorResponse("ទិន្នន័យមិនមិនត្រូវ");
                }

                // Validation
                if (grade.StudentId <= 0)
                    return ErrorResponse("សូមជ្រើសសិស្ស");

                if (string.IsNullOrEmpty(grade.Subject))
                    return ErrorResponse("សូមបញ្ចូលឈ្មោះវិស្ស");

                if (grade.Attendance < 0 || grade.Attendance > 10)
                    return ErrorResponse("វត្តមានត្រូវតើមពី ០ ដល់ ១០");

                if (grade.Assignment < 0 || grade.Assignment > 20)
                    return ErrorResponse("កិច្ចការត្រូវតើមពី ០ ដល់ ២០");

                if (grade.MidTerm < 0 || grade.MidTerm > 30)
                    return ErrorResponse("ពាក់កណ្តាលឆ្នាំត្រូវតើមពី ០ ដល់ ៣០");

                if (grade.FinalExam < 0 || grade.FinalExam > 40)
                    return ErrorResponse("ប្រឡងចុងឆ្នាំត្រូវតើមពី ០ ដល់ ៤០");

                // Calculate total and grade letter
                CalculateGrade(grade);

                grade.RecordedAt = DateTime.Now;
                grade.CreatedBy = PermissionService.GetCurrentUserId(User);

                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();

                // Get student name for logging
                var student = await _context.Students.FindAsync(grade.StudentId);
                var studentName = student?.FullNameKhmer ?? student?.FullName ?? "Unknown";

                // Log activity
                await _activityLogService.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "បង្កើតពិន្ទុ",
                    $"បង្កើតពិន្ទុថ្មី: {studentName} - {grade.Subject}"
                );

                return SuccessResponse("បង្កើតពិន្ទុដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating grade");
                return ErrorResponse("មានបញ្ហាបច្ចេកទេស: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] Grade grade)
        {
            try
            {
                if (!PermissionService.HasPermission(User, "Grade", "Edit"))
                    return RedirectToAction("AccessDenied", "Account");

                var existing = await _context.Grades.FindAsync(grade.Id);
                if (existing == null)
                {
                    return ErrorResponse("រកមិនឃើញពិន្ទុ", 404);
                }

                // Validation
                if (grade.Attendance < 0 || grade.Attendance > 10)
                    return ErrorResponse("វត្តមានត្រូវតើមពី ០ ដល់ ១០");

                if (grade.Assignment < 0 || grade.Assignment > 20)
                    return ErrorResponse("កិច្ចការត្រូវតើមពី ០ ដល់ ២០");

                if (grade.MidTerm < 0 || grade.MidTerm > 30)
                    return ErrorResponse("ពាក់កណ្តាលឆ្នាំត្រូវតើមពី ០ ដល់ ៣០");

                if (grade.FinalExam < 0 || grade.FinalExam > 40)
                    return ErrorResponse("ប្រឡងចុងឆ្នាំត្រូវតើមពី ០ ដល់ ៤០");

                // Update fields
                existing.Subject = grade.Subject;
                existing.Attendance = grade.Attendance;
                existing.Assignment = grade.Assignment;
                existing.MidTerm = grade.MidTerm;
                existing.FinalExam = grade.FinalExam;
                existing.Semester = grade.Semester;
                existing.AcademicYear = grade.AcademicYear;

                // Recalculate total and grade letter
                CalculateGrade(existing);

                await _context.SaveChangesAsync();

                // Get student name for logging
                var student = await _context.Students.FindAsync(existing.StudentId);
                var studentName = student?.FullNameKhmer ?? student?.FullName ?? "Unknown";

                // Log activity
                await _activityLogService.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "កែប្រែពិន្ទុ",
                    $"កែប្រែពិន្ទុថ្មី: {studentName} - {existing.Subject}"
                );

                return SuccessResponse("កែប្រែពិន្ទុដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grade");
                return ErrorResponse("មានបញ្ហាបច្ចេកទេស: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!PermissionService.HasPermission(User, "Grade", "Delete"))
                    return RedirectToAction("AccessDenied", "Account");

                var grade = await _context.Grades
                    .Include(g => g.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);
                
                if (grade == null)
                {
                    return ErrorResponse("រកមិនឃើញពិន្ទុ", 404);
                }

                var gradeInfo = $"{grade.Student?.FullNameKhmer ?? grade.Student?.FullName ?? "Unknown"} - {grade.Subject}";
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "លុបពិន្ទុ",
                    $"លុបពិន្ទុ: {gradeInfo}"
                );

                return SuccessResponse("លុបពិន្ទុដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting grade {GradeId}", id);
                return ErrorResponse("មានបញ្ហាបច្ចេកទេស: " + ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGradeStats()
        {
            try
            {
                var grades = await _context.Grades.ToListAsync();
                
                var total = grades.Count;
                var passed = grades.Count(g => g.Total >= 60);
                var failed = grades.Count(g => g.Total < 60);
                var averageScore = total > 0 ? grades.Average(g => g.Total) : 0;

                var byGradeLetter = grades
                    .GroupBy(g => g.GradeLetter)
                    .ToDictionary(g => g.Key, g => g.Count());

                var bySubject = grades
                    .Where(g => !string.IsNullOrEmpty(g.Subject))
                    .GroupBy(g => g.Subject)
                    .ToDictionary(g => g.Key, g => g.Average(g => g.Total));

                return Json(new {
                    total,
                    gradeDistribution = byGradeLetter,
                    topSubjects = bySubject,
                    averageScore = Math.Round(averageScore, 2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grade stats");
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== HELPER METHODS ====================

        private static void CalculateGrade(Grade grade)
        {
            // Calculate total
            grade.Total = grade.Attendance + grade.Assignment + grade.MidTerm + grade.FinalExam;

            // Calculate grade letter
            if (grade.Total >= 90)
                grade.GradeLetter = "A";
            else if (grade.Total >= 80)
                grade.GradeLetter = "B";
            else if (grade.Total >= 70)
                grade.GradeLetter = "C";
            else if (grade.Total >= 60)
                grade.GradeLetter = "D";
            else if (grade.Total >= 50)
                grade.GradeLetter = "E";
            else
                grade.GradeLetter = "F";
        }
    }
}
