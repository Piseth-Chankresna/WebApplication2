using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services; // កែតម្រូវ namespace ពី Service ទៅ Services

namespace WebApplication2.Controllers
{
    public class StudentController(ApplicationDbContext context, ActivityLogService activityLogService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLogService = activityLogService;

        // ==================== STUDENT LIST ====================
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        // ==================== EXPORT TO EXCEL ====================
        public async Task<IActionResult> ExportToExcel()
        {
            var students = await _context.Students.ToListAsync();

            System.Data.DataTable dt = new();
            dt.Columns.Add("ID");
            dt.Columns.Add("ឈ្មោះ");
            dt.Columns.Add("ភេទ");
            dt.Columns.Add("ថ្ងៃខែឆ្នាំកំណើត");
            dt.Columns.Add("ទីកន្លែងកំណើត");
            dt.Columns.Add("លេខទូរស័ព្ទ");
            dt.Columns.Add("កម្រិតសិក្សា");
            dt.Columns.Add("ជំនាញ");
            dt.Columns.Add("ឆ្នាំសិក្សា");
            dt.Columns.Add("បន្ទប់");
            dt.Columns.Add("អាហារូបករណ៍");
            dt.Columns.Add("វេនសិក្សា");
            dt.Columns.Add("ស្ថានភាព");

            foreach (var s in students)
            {
                dt.Rows.Add(
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
                    s.Status ?? "កំពុងសិក្សា"
                );
            }

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(dt, "Students");
            ws.Columns().AdjustToContents();
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4361ee");
            ws.Row(1).Style.Font.FontColor = XLColor.White;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Student_List.xlsx");
        }

        // ==================== CREATE STUDENT ====================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? Photo)
        {
            try
            {
                // Manual validation
                if (string.IsNullOrEmpty(student.Name))
                {
                    TempData["Error"] = "សូមបញ្ចូលឈ្មោះសិស្ស";
                    return View(student);
                }

                // Generate Student ID
                student.Id = "STU" + DateTime.Now.ToString("yyyyMMddHHmmss");
                student.CreatedAt = DateTime.Now;
                student.Status = "កំពុងសិក្សា";

                // ដោះស្រាយរូបភាព
                if (Photo != null && Photo.Length > 0)
                {
                    // បង្កើត folder uploads បើមិនទាន់មាន
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/students");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // បង្កើតឈ្មោះឯកសារថ្មី (កុំអោយឈ្មោះដូចគ្នា)
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photo.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // រក្សាទុកឯកសារ
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Photo.CopyToAsync(stream);
                    }

                    // រក្សាទុកផ្លូវឯកសារក្នុង database
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

                TempData["Success"] = "បង្កើតសិស្សថ្មីបានជោគជ័យ!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return View(student);
            }
        }

        // ==================== EDIT STUDENT ====================
        public async Task<IActionResult> Edit(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Student updatedStudent, IFormFile? NewPhoto)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

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

                // កែប្រែរូបភាពបើមាន
                if (NewPhoto != null && NewPhoto.Length > 0)
                {
                    // លុបរូបភាពចាស់បើមាន
                    if (!string.IsNullOrEmpty(student.Photo))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + student.Photo);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

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

                TempData["Success"] = "កែប្រែព័ត៌មានសិស្សដោយជោគជ័យ!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // ==================== DELETE STUDENT ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញសិស្ស!" });
                }

                // លុបរូបភាពបើមាន
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

                return Json(new { success = true, message = "លុបសិស្សដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== STUDENT DETAILS ====================
        public async Task<IActionResult> Details(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // ==================== CLASSES MANAGEMENT ====================
        public async Task<IActionResult> Classes()
        {
            var classes = await _context.StudentClasses.ToListAsync();
            return View(classes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass([FromBody] StudentClass classObj)
        {
            try
            {
                if (classObj == null)
                {
                    return BadRequest(new { success = false, message = "ទិន្នន័យមិនត្រឹមត្រូវ" });
                }

                // Validate
                if (string.IsNullOrEmpty(classObj.ClassCode))
                {
                    return BadRequest(new { success = false, message = "សូមបញ្ចូលលេខកូដថ្នាក់" });
                }

                if (string.IsNullOrEmpty(classObj.ClassName))
                {
                    return BadRequest(new { success = false, message = "សូមបញ្ចូលឈ្មោះថ្នាក់" });
                }

                if (string.IsNullOrEmpty(classObj.TeacherName))
                {
                    return BadRequest(new { success = false, message = "សូមបញ្ចូលឈ្មោះគ្រូ" });
                }

                if (string.IsNullOrEmpty(classObj.Room))
                {
                    return BadRequest(new { success = false, message = "សូមបញ្ចូលបន្ទប់" });
                }

                if (string.IsNullOrEmpty(classObj.Time))
                {
                    return BadRequest(new { success = false, message = "សូមបញ្ចូលម៉ោងសិក្សា" });
                }

                // ពិនិត្យមើលថាលេខកូដថ្នាក់មានរួចហើយឬអត់
                var existingClass = await _context.StudentClasses
                    .FirstOrDefaultAsync(c => c.ClassCode == classObj.ClassCode);

                if (existingClass != null)
                {
                    return BadRequest(new { success = false, message = "លេខកូដថ្នាក់នេះមានរួចហើយ" });
                }

                classObj.StudentCount = 0;

                _context.StudentClasses.Add(classObj);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "បង្កើតថ្នាក់ថ្មី",
                    $"បង្កើតថ្នាក់: {classObj.ClassName} (កូដ: {classObj.ClassCode})"
                );

                return Ok(new { success = true, message = "បង្កើតថ្នាក់ថ្មីបានជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "មានបញ្ហាបច្ចេកទេស: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClass([FromBody] StudentClass classObj)
        {
            try
            {
                var existingClass = await _context.StudentClasses.FindAsync(classObj.Id);
                if (existingClass == null)
                {
                    return NotFound(new { success = false, message = "រកមិនឃើញថ្នាក់!" });
                }

                existingClass.ClassCode = classObj.ClassCode;
                existingClass.ClassName = classObj.ClassName;
                existingClass.Room = classObj.Room;
                existingClass.Time = classObj.Time;
                existingClass.TeacherName = classObj.TeacherName;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "កែប្រែថ្នាក់ដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
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
                    return NotFound(new { success = false, message = "រកមិនឃើញថ្នាក់!" });
                }

                _context.StudentClasses.Remove(classObj);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "លុបថ្នាក់ដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==================== GRADES MANAGEMENT ====================
        public async Task<IActionResult> Grades()
        {
            var grades = await _context.Grades
                .Include(g => g.Student)
                .OrderByDescending(g => g.RecordedAt)
                .ToListAsync();
            return View(grades);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrade([FromBody] Grade grade)
        {
            try
            {
                // Calculate total and grade letter
                grade.Total = grade.Attendance + grade.Assignment + grade.MidTerm + grade.FinalExam;

                // កំណត់និទ្ទេស
                if (grade.Total >= 90) grade.GradeLetter = "A";
                else if (grade.Total >= 80) grade.GradeLetter = "B";
                else if (grade.Total >= 70) grade.GradeLetter = "C";
                else if (grade.Total >= 60) grade.GradeLetter = "D";
                else if (grade.Total >= 50) grade.GradeLetter = "E";
                else grade.GradeLetter = "F";

                // ពិនិត្យមើលថាមានពិន្ទុសម្រាប់សិស្សនេះហើយឬនៅ
                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == grade.StudentId && g.Subject == grade.Subject);

                if (existingGrade != null)
                {
                    // Update existing grade
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
                    // Add new grade
                    grade.RecordedAt = DateTime.Now;
                    _context.Grades.Add(grade);
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "រក្សាទុកពិន្ទុ",
                    $"រក្សាទុកពិន្ទុសម្រាប់សិស្ស ID: {grade.StudentId}"
                );

                return Ok(new { success = true, message = "រក្សាទុកពិន្ទុដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==================== ATTENDANCE ====================
        public async Task<IActionResult> CheckAttendance()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        public async Task<IActionResult> Attendance(string? studentId)
        {
            if (!string.IsNullOrEmpty(studentId))
            {
                // បង្ហាញវត្តមានសម្រាប់សិស្សម្នាក់
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return NotFound();
                }

                var attendances = await _context.Attendances
                    .Where(a => a.StudentId == studentId)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                ViewBag.Student = student;
                return View("StudentAttendance", attendances);
            }
            else
            {
                // បង្ហាញវត្តមានទាំងអស់
                var attendances = await _context.Attendances
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
                return View("AttendanceList", attendances);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance([FromBody] List<Attendance> attendances)
        {
            try
            {
                var attendanceCode = "ATT" + DateTime.Now.ToString("yyyyMMddHHmmss");

                foreach (var att in attendances)
                {
                    att.AttendanceCode = attendanceCode;
                    att.RecordedAt = DateTime.Now;
                    _context.Attendances.Add(att);
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "កត់ត្រាវត្តមាន",
                    $"កត់ត្រាវត្តមាន {attendances.Count} នាក់"
                );

                return Ok(new { success = true, message = "រក្សាទុកវត្តមានដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> AttendanceList()
        {
            var attendances = await _context.Attendances
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(attendances);
        }

        // ==================== PAYMENTS ====================
        public async Task<IActionResult> Payment()
        {
            var payments = await _context.Payments
                .Include(p => p.Student)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePayment([FromBody] Payment payment)
        {
            try
            {
                payment.ReceiptNumber = "RCT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                payment.PaymentDate = DateTime.Now;

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Update student amount due
                var student = await _context.Students.FindAsync(payment.StudentId);
                if (student != null)
                {
                    student.AmountDue -= payment.Amount;
                    await _context.SaveChangesAsync();
                }

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "កត់ត្រាការបង់ប្រាក់",
                    $"កត់ត្រាការបង់ប្រាក់: ${payment.Amount} សម្រាប់សិស្ស ID: {payment.StudentId}"
                );

                return Ok(new { success = true, message = "រក្សាទុកការបង់ប្រាក់ដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==================== SCHOLARSHIPS ====================
        public async Task<IActionResult> Scholarship()
        {
            var students = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.Scholarship) && s.Scholarship != "0%")
                .ToListAsync();
            return View(students);
        }

        // ==================== REPORTS ====================
        public async Task<IActionResult> Reports()
        {
            var students = await _context.Students.ToListAsync();
            var payments = await _context.Payments.ToListAsync();
            var attendances = await _context.Attendances.ToListAsync();
            var grades = await _context.Grades.ToListAsync();

            ViewBag.TotalStudents = students.Count;
            ViewBag.TotalPayments = payments.Sum(p => p.Amount);
            ViewBag.AverageAttendance = attendances.Count != 0
                ? (attendances.Count(a => a.Status == "P") * 100 / attendances.Count)
                : 0;
            ViewBag.AverageGrade = grades.Count != 0
                ? grades.Average(g => g.Total)
                : 0;

            return View();
        }

        // ==================== STUDENT CENTER ====================
        public IActionResult StudentCenter()
        {
            return View();
        }

        // ==================== GET STUDENTS FOR API ====================
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)] // Cache 60 វិនាទី
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .Select(s => new {
                        s.Id,
                        s.Name,
                        s.Gender,
                        s.Major,
                        s.Year,
                        s.Room,
                        s.Shift,
                        s.Photo,
                        s.Status,
                        s.AmountDue,
                        s.Scholarship,
                        s.Dob,
                        s.Pob,
                        s.Phone,
                        s.Degree,
                        s.Batch,
                        s.AcademicYear,
                        s.CreatedAt
                    })
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", students.Count.ToString());
                return Json(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudent(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return Json(student);
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.StudentClasses.ToListAsync();
            return Json(classes);
        }

        [HttpGet]
        public async Task<IActionResult> GetGrades()
        {
            var grades = await _context.Grades
                .Include(g => g.Student)
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
            return Json(grades);
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments(string? studentId)
        {
            if (!string.IsNullOrEmpty(studentId))
            {
                var payments = await _context.Payments
                    .Where(p => p.StudentId == studentId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
                return Json(payments);
            }
            else
            {
                var payments = await _context.Payments
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();
                return Json(payments);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance(string? studentId)
        {
            if (!string.IsNullOrEmpty(studentId))
            {
                var attendance = await _context.Attendances
                    .Where(a => a.StudentId == studentId)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
                return Json(attendance);
            }
            else
            {
                var attendance = await _context.Attendances
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
                return Json(attendance);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentStats()
        {
            var total = await _context.Students.CountAsync();
            var male = await _context.Students.CountAsync(s => s.Gender == "ប្រុស" || s.Gender == "Male");
            var female = await _context.Students.CountAsync(s => s.Gender == "ស្រី" || s.Gender == "Female");
            var active = await _context.Students.CountAsync(s => s.Status == "កំពុងសិក្សា");

            return Json(new { total, male, female, active });
        }

        // បន្ថែម Action សម្រាប់បង្ហាញរូបភាព
        [HttpGet]
        public IActionResult GetStudentImage(string studentId)
        {
            var student = _context.Students.Find(studentId);
            if (student == null || string.IsNullOrEmpty(student.Photo))
            {
                return NotFound();
            }

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + student.Photo);
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }

            var imageFileStream = System.IO.File.OpenRead(imagePath);
            var fileExtension = Path.GetExtension(student.Photo).Replace(".", "");
            return File(imageFileStream, "image/" + fileExtension);
        }

        // បន្ថែម Action សម្រាប់យកពិន្ទុរបស់សិស្សម្នាក់ៗ
        [HttpGet]
        public async Task<IActionResult> GetStudentGrades(string studentId)
        {
            var grades = await _context.Grades
                .Where(g => g.StudentId == studentId)
                .OrderByDescending(g => g.RecordedAt)
                .ToListAsync();
            return Json(grades);
        }
    }
}