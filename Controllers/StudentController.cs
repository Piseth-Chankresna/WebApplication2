using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using ClosedXML.Excel;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class StudentController(ApplicationDbContext context, ActivityLogService activityLogService, ILogger<StudentController> logger) : BaseController
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLogService = activityLogService;
        private readonly ILogger<StudentController> _logger = logger;

        // ==================== VIEWS ====================

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound();
                }
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit page for student {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound();
                }
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Details page for student {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Classes()
        {
            return View();
        }

        public IActionResult Grades()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckAttendance(string? studentId = null)
        {
            try
            {
                ViewBag.SelectedDate = DateTime.Today.ToString("yyyy-MM-dd");

                if (!string.IsNullOrEmpty(studentId))
                {
                    var student = await _context.Students.FindAsync(studentId);
                    ViewBag.SelectedStudent = student;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading CheckAttendance page");
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public Task<IActionResult> AttendanceList(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

                return Task.FromResult<IActionResult>(View());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading AttendanceList page");
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រ";
                return Task.FromResult<IActionResult>(RedirectToAction("Index"));
            }
        }

        public Task<IActionResult> Payment()
        {
            return Task.FromResult<IActionResult>(View());
        }

        public Task<IActionResult> Scholarship()
        {
            return Task.FromResult<IActionResult>(View());
        }

        public Task<IActionResult> Reports()
        {
            return Task.FromResult<IActionResult>(View());
        }

        public IActionResult StudentCenter()
        {
            return View();
        }

        // ==================== STUDENT API ====================

        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new {
                        s.Id,
                        s.Name,
                        s.Gender,
                        s.Dob,
                        s.Pob,
                        s.Phone,
                        s.Degree,
                        s.Major,
                        s.Year,
                        s.Room,
                        s.Scholarship,
                        s.Shift,
                        s.Photo,
                        s.Batch,
                        s.AcademicYear,
                        s.AmountDue,
                        s.Status,
                        s.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                return ErrorResponse("មានបញ្ហាក្នុងការទាញយកទិន្នន័យ: " + ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentStats()
        {
            try
            {
                var total = await _context.Students.CountAsync();
                var male = await _context.Students.CountAsync(s => s.Gender == "ប្រុស" || s.Gender == "Male");
                var female = await _context.Students.CountAsync(s => s.Gender == "ស្រី" || s.Gender == "Female");
                var active = await _context.Students.CountAsync(s => s.Status == "កំពុងសិក្សា");

                return Json(new { total, male, female, active });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student stats");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Student student, IFormFile? Photo)
        {
            try
            {
                // Validation
                if (string.IsNullOrEmpty(student.Name))
                {
                    return ErrorResponse("សូមបញ្ចូលឈ្មោះសិស្ស");
                }

                if (string.IsNullOrEmpty(student.Gender))
                {
                    return ErrorResponse("សូមជ្រើសរើសភេទ");
                }

                if (string.IsNullOrEmpty(student.Dob))
                {
                    return ErrorResponse("សូមបញ្ចូលថ្ងៃខែឆ្នាំកំណើត");
                }

                if (string.IsNullOrEmpty(student.Phone))
                {
                    return ErrorResponse("សូមបញ្ចូលលេខទូរស័ព្ទ");
                }

                // Generate Student ID
                student.Id = "STU" + DateTime.Now.ToString("yyyyMMddHHmmss");
                student.CreatedAt = DateTime.Now;
                student.Status = "កំពុងសិក្សា";

                // Handle Photo Upload
                if (Photo != null && Photo.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/students");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photo.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Photo.CopyToAsync(stream);
                    }

                    student.Photo = "/uploads/students/" + fileName;
                }

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "បង្កើតសិស្សថ្មី",
                    $"បង្កើតសិស្ស: {student.Name} (ID: {student.Id})"
                );

                return Json(new
                {
                    success = true,
                    message = "បង្កើតសិស្សថ្មីបានជោគជ័យ!",
                    redirectUrl = "/Student/Index"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return ErrorResponse("មានបញ្ហា: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Student updatedStudent, IFormFile? NewPhoto)
        {
            try
            {
                var student = await _context.Students.FindAsync(updatedStudent.Id);
                if (student == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញសិស្ស" });
                }

                // Update fields
                student.Name = updatedStudent.Name;
                student.Gender = updatedStudent.Gender;
                student.Dob = updatedStudent.Dob;
                student.Pob = updatedStudent.Pob;
                student.Phone = updatedStudent.Phone;
                student.Degree = updatedStudent.Degree;
                student.Major = updatedStudent.Major;
                student.Year = updatedStudent.Year;
                student.Room = updatedStudent.Room;
                student.Scholarship = updatedStudent.Scholarship;
                student.Shift = updatedStudent.Shift;
                student.Status = updatedStudent.Status;
                student.Batch = updatedStudent.Batch;
                student.AcademicYear = updatedStudent.AcademicYear;
                student.AmountDue = updatedStudent.AmountDue;

                // Handle new photo
                if (NewPhoto != null && NewPhoto.Length > 0)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(student.Photo))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + student.Photo);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new photo
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/students");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(NewPhoto.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await NewPhoto.CopyToAsync(stream);
                    }

                    student.Photo = "/uploads/students/" + fileName;
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "កែប្រែព័ត៌មានសិស្ស",
                    $"កែប្រែសិស្ស: {student.Name} (ID: {student.Id})"
                );

                return Json(new { success = true, message = "កែប្រែព័ត៌មានសិស្សដោយជោគជ័យ!", redirectUrl = "/Student/Index" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "មានបញ្ហា: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return ErrorResponse("រកមិនឃើញសិស្ស", 404);
                }

                // Delete photo
                if (!string.IsNullOrEmpty(student.Photo))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + student.Photo);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                var studentName = student.Name;
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "លុបសិស្ស",
                    $"លុបសិស្ស: {studentName} (ID: {id})"
                );

                return SuccessResponse("លុបសិស្សដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== CLASSES API ====================

        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _context.StudentClasses
                    .OrderBy(c => c.ClassCode)
                    .ToListAsync();
                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting classes");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass([FromBody] StudentClass classObj)
        {
            try
            {
                if (classObj == null)
                {
                    return ErrorResponse("ទិន្នន័យមិនត្រឹមត្រូវ");
                }

                // Validate
                if (string.IsNullOrEmpty(classObj.ClassCode))
                    return ErrorResponse("សូមបញ្ចូលលេខកូដថ្នាក់");
                if (string.IsNullOrEmpty(classObj.ClassName))
                    return ErrorResponse("សូមបញ្ចូលឈ្មោះថ្នាក់");
                if (string.IsNullOrEmpty(classObj.TeacherName))
                    return ErrorResponse("សូមបញ្ចូលឈ្មោះគ្រូ");
                if (string.IsNullOrEmpty(classObj.Room))
                    return ErrorResponse("សូមបញ្ចូលបន្ទប់");
                if (string.IsNullOrEmpty(classObj.Time))
                    return ErrorResponse("សូមបញ្ចូលម៉ោងសិក្សា");

                // Check duplicate
                var existing = await _context.StudentClasses
                    .FirstOrDefaultAsync(c => c.ClassCode == classObj.ClassCode);
                if (existing != null)
                {
                    return ErrorResponse("លេខកូដថ្នាក់នេះមានរួចហើយ");
                }

                classObj.StudentCount = 0;
                _context.StudentClasses.Add(classObj);
                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync(0, "System", "បង្កើតថ្នាក់ថ្មី",
                    $"បង្កើតថ្នាក់: {classObj.ClassName}");

                return SuccessResponse("បង្កើតថ្នាក់ថ្មីបានជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating class");
                return ErrorResponse("មានបញ្ហាបច្ចេកទេស: " + ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClass([FromBody] StudentClass classObj)
        {
            try
            {
                var existing = await _context.StudentClasses.FindAsync(classObj.Id);
                if (existing == null)
                {
                    return ErrorResponse("រកមិនឃើញថ្នាក់", 404);
                }

                existing.ClassCode = classObj.ClassCode;
                existing.ClassName = classObj.ClassName;
                existing.Room = classObj.Room;
                existing.Time = classObj.Time;
                existing.TeacherName = classObj.TeacherName;

                await _context.SaveChangesAsync();
                return SuccessResponse("កែប្រែថ្នាក់ដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class {ClassId}", classObj.Id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                var classObj = await _context.StudentClasses.FindAsync(id);
                if (classObj == null)
                {
                    return ErrorResponse("រកមិនឃើញថ្នាក់", 404);
                }

                _context.StudentClasses.Remove(classObj);
                await _context.SaveChangesAsync();
                return SuccessResponse("លុបថ្នាក់ដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting class {ClassId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== ATTENDANCE API ====================

        [HttpGet]
        public async Task<IActionResult> GetAttendanceRecords(DateTime? startDate, DateTime? endDate, string? studentId = null)
        {
            try
            {
                var query = _context.Attendances
                    .Include(a => a.Student)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Date <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(studentId))
                {
                    query = query.Where(a => a.StudentId == studentId);
                }

                var records = await query
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.Id,
                        a.StudentId,
                        a.StudentName,
                        StudentPhoto = a.Student != null ? a.Student.Photo : null,
                        StudentMajor = a.Student != null ? a.Student.Major : "",
                        StudentYear = a.Student != null ? a.Student.Year : "",
                        a.Date,
                        a.Status,
                        a.Note,
                        StatusText = a.Status == "P" ? "វត្តមាន" : a.Status == "L" ? "មកយឺត" : "អវត្តមាន",
                        StatusColor = a.Status == "P" ? "present" : a.Status == "L" ? "late" : "absent"
                    })
                    .ToListAsync();

                return Json(new { success = true, data = records });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance records");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceStats(DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;

                var stats = await _context.Attendances
                    .Where(a => a.Date == targetDate)
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key ?? "", Count = g.Count() })
                    .ToDictionaryAsync(g => g.Status, g => g.Count);

                return Json(new
                {
                    success = true,
                    present = stats.GetValueOrDefault("P", 0),
                    late = stats.GetValueOrDefault("L", 0),
                    absent = stats.GetValueOrDefault("A", 0),
                    total = stats.Values.Sum()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance([FromBody] List<Attendance> attendances)
        {
            try
            {
                // ពិនិត្យមើលទិន្នន័យ
                if (attendances == null || attendances.Count == 0)
                {
                    return Json(new { success = false, message = "មិនមានទិន្នន័យវត្តមានត្រូវបានផ្ញើមក" });
                }

                Console.WriteLine($"Received {attendances.Count} attendance records");

                var firstAttendance = attendances.FirstOrDefault();
                var date = firstAttendance?.Date ?? DateTime.Today;
                var attendanceCode = "ATT" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Delete old records for this date
                var oldRecords = await _context.Attendances
                    .Where(a => a.Date == date)
                    .ToListAsync();

                if (oldRecords.Count != 0)
                {
                    _context.Attendances.RemoveRange(oldRecords);
                    Console.WriteLine($"Deleted {oldRecords.Count} old records");
                }

                // Add new records
                foreach (var att in attendances)
                {
                    att.AttendanceCode = attendanceCode;
                    att.RecordedAt = DateTime.Now;
                    _context.Attendances.Add(att);
                    Console.WriteLine($"Adding: {att.StudentId} - {att.Status}");
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Saved successfully");

                await _activityLogService.LogAsync(0, "System", "កត់ត្រាវត្តមាន",
                    $"កត់ត្រាវត្តមាន {attendances.Count} នាក់ សម្រាប់ថ្ងៃទី {date:dd/MM/yyyy}");

                return Json(new { success = true, message = "រក្សាទុកវត្តមានដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving attendance: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendance(int id, DateTime date, string status, string note)
        {
            try
            {
                Console.WriteLine($"Updating attendance ID: {id}, Date: {date}, Status: {status}, Note: {note}");

                var attendance = await _context.Attendances.FindAsync(id);
                if (attendance == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញកំណត់ត្រាវត្តមាន" });
                }

                // Update fields
                attendance.Date = date;
                attendance.Status = status;
                attendance.Note = note ?? "";
                attendance.RecordedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "កែប្រែវត្តមានដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating attendance: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            try
            {
                var attendance = await _context.Attendances.FindAsync(id);
                if (attendance == null)
                {
                    return ErrorResponse("រកមិនឃើញកំណត់ត្រា", 404);
                }

                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
                return SuccessResponse("លុបកំណត់ត្រាវត្តមានដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance {AttendanceId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== GRADES API ====================

        [HttpGet]
        public async Task<IActionResult> GetGrades(string? studentId = null)
        {
            try
            {
                var query = _context.Grades
                    .Include(g => g.Student)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(studentId))
                {
                    query = query.Where(g => g.StudentId == studentId);
                }

                var grades = await query
                    .OrderByDescending(g => g.RecordedAt)
                    .Select(g => new {
                        g.Id,
                        g.StudentId,
                        StudentName = g.Student != null ? g.Student.Name : "",
                        g.Attendance,
                        g.Assignment,
                        g.MidTerm,
                        g.FinalExam,
                        g.Total,
                        g.GradeLetter,
                        g.Semester,
                        g.AcademicYear,
                        g.RecordedAt
                    })
                    .ToListAsync();

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", grades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grades");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrade([FromBody] Grade grade)
        {
            try
            {
                // Calculate total and grade letter
                grade.Total = grade.Attendance + grade.Assignment + grade.MidTerm + grade.FinalExam;

                if (grade.Total >= 90) grade.GradeLetter = "A";
                else if (grade.Total >= 80) grade.GradeLetter = "B";
                else if (grade.Total >= 70) grade.GradeLetter = "C";
                else if (grade.Total >= 60) grade.GradeLetter = "D";
                else if (grade.Total >= 50) grade.GradeLetter = "E";
                else grade.GradeLetter = "F";

                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == grade.StudentId && g.Subject == grade.Subject);

                if (existingGrade != null)
                {
                    existingGrade.Attendance = grade.Attendance;
                    existingGrade.Assignment = grade.Assignment;
                    existingGrade.MidTerm = grade.MidTerm;
                    existingGrade.FinalExam = grade.FinalExam;
                    existingGrade.Total = grade.Total;
                    existingGrade.GradeLetter = grade.GradeLetter;
                    existingGrade.RecordedAt = DateTime.Now;
                }
                else
                {
                    grade.RecordedAt = DateTime.Now;
                    _context.Grades.Add(grade);
                }

                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync(0, "System", "រក្សាទុកពិន្ទុ",
                    $"រក្សាទុកពិន្ទុសម្រាប់សិស្ស ID: {grade.StudentId}");

                return SuccessResponse("រក្សាទុកពិន្ទុដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving grade");
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== PAYMENTS API ====================
        [HttpGet]
        public async Task<IActionResult> GetPayments(string? studentId = null)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Student)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(studentId))
                {
                    query = query.Where(p => p.StudentId == studentId);
                }

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new
                    {
                        p.Id,
                        p.StudentId,
                        p.StudentName,
                        StudentPhoto = p.Student != null ? p.Student.Photo : null,
                        StudentMajor = p.Student != null ? p.Student.Major : "",
                        StudentYear = p.Student != null ? p.Student.Year : "",
                        p.ReceiptNumber,
                        p.Amount,
                        p.PaymentDate,
                        p.PaymentMethod,
                        p.Semester,
                        p.AcademicYear,
                        p.Status,
                        p.Note
                    })
                    .ToListAsync();

                return Json(new { success = true, data = payments });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPayments: {ex.Message}");
                return Json(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentStats()
        {
            try
            {
                var today = DateTime.Today;
                var sixMonthsAgo = today.AddMonths(-6);

                var monthlyStats = await _context.Payments
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

                return Json(new { success = true, data = monthlyStats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePayment([FromBody] Payment payment)
        {
            try
            {
                Console.WriteLine("========== SAVE PAYMENT START ==========");
                Console.WriteLine($"StudentId: {payment?.StudentId}");
                Console.WriteLine($"StudentName: {payment?.StudentName}");
                Console.WriteLine($"Amount: {payment?.Amount}");
                Console.WriteLine($"PaymentDate: {payment?.PaymentDate}");
                Console.WriteLine($"PaymentMethod: {payment?.PaymentMethod}");
                Console.WriteLine($"Semester: {payment?.Semester}");
                Console.WriteLine($"AcademicYear: {payment?.AcademicYear}");
                Console.WriteLine($"Note: {payment?.Note}");

                if (payment == null)
                {
                    Console.WriteLine("ERROR: Payment object is null");
                    return Json(new { success = false, message = "ទិន្នន័យមិនត្រឹមត្រូវ" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(payment.StudentId))
                {
                    Console.WriteLine("ERROR: StudentId is empty");
                    return Json(new { success = false, message = "សូមជ្រើសរើសសិស្ស" });
                }

                if (payment.Amount <= 0)
                {
                    Console.WriteLine("ERROR: Amount is invalid");
                    return Json(new { success = false, message = "សូមបញ្ចូលចំនួនទឹកប្រាក់អោយបានត្រឹមត្រូវ" });
                }

                // Generate receipt number
                payment.ReceiptNumber = "RCT" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                payment.PaymentDate = DateTime.Now;
                payment.Status = "បង់រួច";

                Console.WriteLine($"Generated Receipt: {payment.ReceiptNumber}");

                // Add to database
                _context.Payments.Add(payment);
                Console.WriteLine("Added payment to context");

                int saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync result: {saveResult} records saved");

                // Update student amount due
                var student = await _context.Students.FindAsync(payment.StudentId);
                if (student != null)
                {
                    Console.WriteLine($"Student found: {student.Name}, Current AmountDue: {student.AmountDue}");

                    student.AmountDue -= payment.Amount;
                    Console.WriteLine($"New AmountDue: {student.AmountDue}");

                    await _context.SaveChangesAsync();
                    Console.WriteLine("Student amount due updated");
                }
                else
                {
                    Console.WriteLine($"WARNING: Student with ID {payment.StudentId} not found");
                }

                // Log activity
                if (_activityLogService != null)
                {
                    await _activityLogService.LogAsync(0, "System", "កត់ត្រាការបង់ប្រាក់",
                        $"កត់ត្រាការបង់ប្រាក់: ${payment.Amount} សម្រាប់សិស្ស ID: {payment.StudentId}");
                }

                Console.WriteLine("========== SAVE PAYMENT SUCCESS ==========");
                return Json(new { success = true, message = "រក្សាទុកការបង់ប្រាក់ដោយជោគជ័យ!" });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("========== DB UPDATE EXCEPTION ==========");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Get detailed error message
                string errorMessage = "មានបញ្ហាក្នុងការរក្សាទុកទិន្នន័យ";
                if (ex.InnerException != null)
                {
                    errorMessage += ": " + ex.InnerException.Message;
                }

                return Json(new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== GENERAL EXCEPTION ==========");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new { success = false, message = "មានបញ្ហាបច្ចេកទេស: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return ErrorResponse("រកមិនឃើញកំណត់ត្រាការបង់ប្រាក់", 404);
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                return SuccessResponse("លុបកំណត់ត្រាការបង់ប្រាក់ដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment {PaymentId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== SCHOLARSHIP API ====================

        [HttpGet]
        public async Task<IActionResult> GetScholarshipStudents()
        {
            try
            {
                var students = await _context.Students
                    .Where(s => !string.IsNullOrEmpty(s.Scholarship) && s.Scholarship != "0%")
                    .OrderByDescending(s => s.Scholarship)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Photo,
                        s.Major,
                        s.Year,
                        s.Shift,
                        s.Scholarship,
                        s.AmountDue,
                        s.Status
                    })
                    .ToListAsync();

                return SuccessResponse("ទាញយកទិន្នន័យបានជោគជ័យ", students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scholarship students");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScholarshipStats()
        {
            try
            {
                var total = await _context.Students.CountAsync();
                var withScholarship = await _context.Students
                    .CountAsync(s => !string.IsNullOrEmpty(s.Scholarship) && s.Scholarship != "0%");

                var stats = new
                {
                    total,
                    withScholarship,
                    withoutScholarship = total - withScholarship,
                    percentage = total > 0 ? (withScholarship * 100 / total) : 0
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scholarship stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateScholarship(string id, string scholarship)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return ErrorResponse("រកមិនឃើញសិស្ស", 404);
                }

                student.Scholarship = scholarship;
                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync(0, "System", "កែប្រែអាហារូបករណ៍",
                    $"កែប្រែអាហារូបករណ៍សម្រាប់សិស្ស: {student.Name}");

                return SuccessResponse("កែប្រែអាហារូបករណ៍ដោយជោគជ័យ!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scholarship for student {StudentId}", id);
                return ErrorResponse(ex.Message, 500);
            }
        }

        // ==================== EXPORT FUNCTIONS ====================

        [HttpGet]
        public async Task<IActionResult> ExportAttendance(DateTime startDate, DateTime endDate)
        {
            try
            {
                var attendances = await _context.Attendances
                    .Where(a => a.Date >= startDate && a.Date <= endDate)
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.StudentName)
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Attendance");

                // Headers
                ws.Cell(1, 1).Value = "កាលបរិច្ឆេទ";
                ws.Cell(1, 2).Value = "ឈ្មោះសិស្ស";
                ws.Cell(1, 3).Value = "លេខសម្គាល់";
                ws.Cell(1, 4).Value = "ស្ថានភាព";
                ws.Cell(1, 5).Value = "កំណត់ចំណាំ";

                int row = 2;
                foreach (var a in attendances)
                {
                    ws.Cell(row, 1).Value = a.Date.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = a.StudentName;
                    ws.Cell(row, 3).Value = a.StudentId;
                    ws.Cell(row, 4).Value = a.Status == "P" ? "វត្តមាន" : a.Status == "L" ? "មកយឺត" : "អវត្តមាន";
                    ws.Cell(row, 5).Value = a.Note ?? "";
                    row++;
                }

                ws.RangeUsed().SetAutoFilter();
                ws.Columns().AdjustToContents();
                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#06b6d4");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Attendance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportStudents()
        {
            try
            {
                var students = await _context.Students.ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Students");

                // Headers
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "ឈ្មោះ";
                ws.Cell(1, 3).Value = "ភេទ";
                ws.Cell(1, 4).Value = "ថ្ងៃខែឆ្នាំកំណើត";
                ws.Cell(1, 5).Value = "ទីកន្លែងកំណើត";
                ws.Cell(1, 6).Value = "លេខទូរស័ព្ទ";
                ws.Cell(1, 7).Value = "កម្រិតសិក្សា";
                ws.Cell(1, 8).Value = "ជំនាញ";
                ws.Cell(1, 9).Value = "ឆ្នាំសិក្សា";
                ws.Cell(1, 10).Value = "បន្ទប់";
                ws.Cell(1, 11).Value = "អាហារូបករណ៍";
                ws.Cell(1, 12).Value = "វេនសិក្សា";
                ws.Cell(1, 13).Value = "ស្ថានភាព";

                int row = 2;
                foreach (var s in students)
                {
                    ws.Cell(row, 1).Value = s.Id;
                    ws.Cell(row, 2).Value = s.Name;
                    ws.Cell(row, 3).Value = s.Gender;
                    ws.Cell(row, 4).Value = s.Dob;
                    ws.Cell(row, 5).Value = s.Pob;
                    ws.Cell(row, 6).Value = s.Phone;
                    ws.Cell(row, 7).Value = s.Degree;
                    ws.Cell(row, 8).Value = s.Major;
                    ws.Cell(row, 9).Value = s.Year;
                    ws.Cell(row, 10).Value = s.Room;
                    ws.Cell(row, 11).Value = s.Scholarship;
                    ws.Cell(row, 12).Value = s.Shift;
                    ws.Cell(row, 13).Value = s.Status ?? "កំពុងសិក្សា";
                    row++;
                }

                ws.RangeUsed().SetAutoFilter();
                ws.Columns().AdjustToContents();
                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#06b6d4");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Students_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting students");
                return ErrorResponse(ex.Message, 500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPayments(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Student)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= endDate.Value);
                }

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Payments");

                // Headers
                ws.Cell(1, 1).Value = "វិក្កយបត្រ";
                ws.Cell(1, 2).Value = "ឈ្មោះសិស្ស";
                ws.Cell(1, 3).Value = "លេខសម្គាល់";
                ws.Cell(1, 4).Value = "កាលបរិច្ឆេទ";
                ws.Cell(1, 5).Value = "ទឹកប្រាក់";
                ws.Cell(1, 6).Value = "វិធីបង់";
                ws.Cell(1, 7).Value = "ឆមាស";
                ws.Cell(1, 8).Value = "កំណត់ចំណាំ";

                int row = 2;
                foreach (var p in payments)
                {
                    ws.Cell(row, 1).Value = p.ReceiptNumber;
                    ws.Cell(row, 2).Value = p.StudentName;
                    ws.Cell(row, 3).Value = p.StudentId;
                    ws.Cell(row, 4).Value = p.PaymentDate.ToString("dd/MM/yyyy");
                    ws.Cell(row, 5).Value = p.Amount;
                    ws.Cell(row, 6).Value = p.PaymentMethod == "cash" ? "សាច់ប្រាក់" : p.PaymentMethod == "aba" ? "ABA Pay" : "Wing";
                    ws.Cell(row, 7).Value = p.Semester;
                    ws.Cell(row, 8).Value = p.Note ?? "";
                    row++;
                }

                ws.RangeUsed().SetAutoFilter();
                ws.Columns().AdjustToContents();
                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#10b981");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Payments_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting payments");
                return ErrorResponse(ex.Message, 500);
            }
        }
    }
}