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
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
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
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return RedirectToAction("Index");
            }
        }

        // ==================== API ENDPOINTS ====================

        [HttpGet]
        public async Task<IActionResult> GetGrades(string? studentId = null, string? subject = null, string? semester = null, string? academicYear = null)
        {
            try
            {
                var query = _context.Grades
                    .Include(g => g.Student)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(studentId) && int.TryParse(studentId, out int sid))
                {
                    query = query.Where(g => g.StudentId == sid);
                }

                if (!string.IsNullOrEmpty(subject))
                {
                    query = query.Where(g => g.Subject != null && g.Subject.Contains(subject));
                }

                if (!string.IsNullOrEmpty(semester))
                {
                    query = query.Where(g => g.Semester == semester);
                }

                if (!string.IsNullOrEmpty(academicYear))
                {
                    query = query.Where(g => g.AcademicYear == academicYear);
                }

                var grades = await query
                    .OrderByDescending(g => g.RecordedAt)
                    .Select(g => new {
                        g.Id,
                        g.StudentId,
                        StudentName = g.Student != null ? g.Student.FullName : "",
                        StudentIdNumber = "STU" + g.StudentId.ToString("D6"),
                        g.Subject,
                        g.Attendance,
                        g.Assignment,
                        g.MidTerm,
                        g.FinalExam,
                        g.Total,
                        g.GradeLetter,
                        g.Semester,
                        g.AcademicYear,
                        g.RecordedAt,
                        GradePoint = GetGradePoint(g.GradeLetter),
                        Status = GetGradeStatus(g.GradeLetter)
                    })
                    .ToListAsync();

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", grades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grades");
                return ErrorResponse("មានបញ្ហាក្នុងការទាញយកទិន្នន័យ: " + ex.Message, 500);
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
                    return ErrorResponse("រកមិនឃើញពិន្ទុ", 404);
                }

                var gradeDetails = new {
                    grade.Id,
                    grade.StudentId,
                    StudentName = grade.Student != null ? grade.Student.FullName : "",
                    StudentIdNumber = "STU" + grade.StudentId.ToString("D6"),
                    grade.Subject,
                    grade.Attendance,
                    grade.Assignment,
                    grade.MidTerm,
                    grade.FinalExam,
                    grade.Total,
                    grade.GradeLetter,
                    grade.Semester,
                    grade.AcademicYear,
                    grade.RecordedAt,
                    GradePoint = GetGradePoint(grade.GradeLetter),
                    Status = GetGradeStatus(grade.GradeLetter)
                };

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", gradeDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grade {GradeId}", id);
                return ErrorResponse("មានបញ្ហាក្នុងការទាញយកទិន្នន័យ: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Grade grade)
        {
            if (!PermissionService.HasPermission(User, "Grade", "Create"))
                return ErrorResponse("អ្នកគ្មានសិទ្ធិបញ្ចូលពិន្ទុ", 403);
                
            try
            {
                if (grade == null)
                {
                    return ErrorResponse("ទិន្នន័យមិនត្រឹមត្រូវ");
                }

                // Validation
                if (grade.StudentId <= 0)
                    return ErrorResponse("សូមជ្រើសរើសសិស្ស");
                
                if (string.IsNullOrEmpty(grade.Subject))
                    return ErrorResponse("សូមបញ្ចូលឈ្មោះមុខវិជ្ជា");

                // Check if student exists
                var student = await _context.Students.FindAsync(grade.StudentId);
                if (student == null)
                {
                    return ErrorResponse("រកមិនឃើញសិស្សនេះទេ");
                }

                // Check for duplicate grade
                var existing = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == grade.StudentId && 
                                           g.Subject == grade.Subject && 
                                           g.Semester == grade.Semester && 
                                           g.AcademicYear == grade.AcademicYear);
                
                if (existing != null)
                {
                    return ErrorResponse("សិស្សនេះមានពិន្ទុសម្រាប់មុខវិជ្ជានេះក្នុងឆមាស/ឆ្នាំសិក្សានេះរួចហើយ");
                }

                // Calculate total and grade letter
                CalculateGrade(grade);

                grade.RecordedAt = DateTime.Now;

                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "បញ្ចូលពិន្ទុ",
                    $"បញ្ចូលពិន្ទុសម្រាប់សិស្ស {student.FullName} មុខវិជ្ជា {grade.Subject}"
                );

                return SuccessResponse("បញ្ចូលពិន្ទុដោយជោគជ័យ!");
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
            if (!PermissionService.HasPermission(User, "Grade", "Edit"))
                return ErrorResponse("អ្នកគ្មានសិទ្ធិកែប្រែពិន្ទុ", 403);
                
            try
            {
                var existing = await _context.Grades.FindAsync(grade.Id);
                if (existing == null)
                {
                    return ErrorResponse("រកមិនឃើញពិន្ទុ", 404);
                }

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
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "កែប្រែពិន្ទុ",
                    $"កែប្រែពិន្ទុសម្រាប់សិស្ស {student?.FullName} មុខវិជ្ជា {existing.Subject}"
                );

                return SuccessResponse("កែប្រែពិន្ទុដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grade {GradeId}", grade.Id);
                return ErrorResponse("មានបញ្ហាបច្ចេកទេស: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!PermissionService.HasPermission(User, "Grade", "Delete"))
                return ErrorResponse("អ្នកគ្មានសិទ្ធិលុបពិន្ទុ", 403);
                
            try
            {
                var grade = await _context.Grades
                    .Include(g => g.Student)
                    .FirstOrDefaultAsync(g => g.Id == id);
                
                if (grade == null)
                {
                    return ErrorResponse("រកមិនឃើញពិន្ទុ", 404);
                }

                var gradeInfo = $"{grade.Student?.FullName} - {grade.Subject}";
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
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
                var total = await _context.Grades.CountAsync();
                var byGradeLetter = await _context.Grades
                    .GroupBy(g => g.GradeLetter)
                    .Select(g => new { Grade = g.Key ?? "", Count = g.Count() })
                    .ToDictionaryAsync(g => g.Grade, g => g.Count);

                var bySubject = await _context.Grades
                    .Where(g => g.Subject != null)
                    .GroupBy(g => g.Subject)
                    .Select(g => new { Subject = g.Key, Count = g.Count(), Average = g.Average(x => x.Total) })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToListAsync();

                var averageScore = await _context.Grades.AverageAsync(g => g.Total);

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
                return ErrorResponse("មានបញ្ហាក្នុងការទាញយកស្ថិតិ: " + ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentGrades(int studentId)
        {
            try
            {
                var grades = await _context.Grades
                    .Where(g => g.StudentId == studentId)
                    .OrderByDescending(g => g.AcademicYear)
                    .ThenBy(g => g.Semester)
                    .Select(g => new {
                        g.Id,
                        g.Subject,
                        g.Attendance,
                        g.Assignment,
                        g.MidTerm,
                        g.FinalExam,
                        g.Total,
                        g.GradeLetter,
                        g.Semester,
                        g.AcademicYear,
                        GradePoint = GetGradePoint(g.GradeLetter),
                        Status = GetGradeStatus(g.GradeLetter)
                    })
                    .ToListAsync();

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", grades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grades for student {StudentId}", studentId);
                return ErrorResponse("មានបញ្ហាក្នុងការទាញយកទិន្នន័យ: " + ex.Message, 500);
            }
        }

        // ==================== HELPER METHODS ====================

        private void CalculateGrade(Grade grade)
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

        private decimal GetGradePoint(string? gradeLetter)
        {
            return gradeLetter switch
            {
                "A" => 4.0m,
                "B" => 3.0m,
                "C" => 2.0m,
                "D" => 1.0m,
                "E" => 0.5m,
                "F" => 0.0m,
                _ => 0.0m
            };
        }

        private string GetGradeStatus(string? gradeLetter)
        {
            return gradeLetter switch
            {
                "A" => "ល្អណាស់",
                "B" => "ល្អ",
                "C" => "មធ្យម",
                "D" => "ខ្សោយ",
                "E" => "ខ្សោយណាស់",
                "F" => "ធ្លាក់",
                _ => "មិនកំណត់"
            };
        }
    }
}
