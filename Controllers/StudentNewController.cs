using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;
using System.Security.Claims;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class StudentNewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivityLogService _activityLogService;
        private readonly ILogger<StudentNewController> _logger;

        public StudentNewController(
            ApplicationDbContext context, 
            ActivityLogService activityLogService,
            ILogger<StudentNewController> logger)
        {
            _context = context;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        // GET: /StudentNew/
        public async Task<IActionResult> Index()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.Class)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                return View(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student list");
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទិន្នន័យសិស្ស";
                return View(new List<Student>());
            }
        }

        // GET: /StudentNew/Create
        public IActionResult Create()
        {
            ViewBag.Classes = _context.StudentClasses
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .ToList();
            return View();
        }

        // POST: /StudentNew/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? Photo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check for duplicate email
                    var existingStudent = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email == student.Email);
                    
                    if (existingStudent != null)
                    {
                        ModelState.AddModelError("Email", "អ៊ីមែលនេះត្រូវបានប្រើរួចហើយ");
                        ViewBag.Classes = _context.StudentClasses
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.ClassName)
                            .ToList();
                        return View(student);
                    }

                    // Handle photo upload
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

                    student.CreatedAt = DateTime.Now;
                    student.CreatedBy = GetCurrentUserId();
                    student.IsActive = true;

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Update class student count
                    if (student.ClassId.HasValue)
                    {
                        var studentClass = await _context.StudentClasses.FindAsync(student.ClassId);
                        if (studentClass != null)
                        {
                            studentClass.StudentCount++;
                            await _context.SaveChangesAsync();
                        }
                    }

                    await _activityLogService.LogAsync(
                        GetCurrentUserId(),
                        "Student",
                        "Create",
                        $"បង្កើតសិស្សថ្មី: {student.FullName}"
                    );

                    TempData["Success"] = "បង្កើតសិស្សថ្មីបានជោគជ័យ!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Classes = _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                TempData["Error"] = "មានបញ្ហាក្នុងការបង្កើតសិស្ស: " + ex.Message;
                ViewBag.Classes = _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();
                return View(student);
            }
        }

        // GET: /StudentNew/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

                ViewBag.Classes = _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for student {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកទំព័រកែប្រែ";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /StudentNew/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student, IFormFile? Photo)
        {
            try
            {
                if (id != student.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var existingStudent = await _context.Students.FindAsync(id);
                    if (existingStudent == null)
                    {
                        return NotFound();
                    }

                    // Check for duplicate email (excluding current student)
                    var duplicateEmail = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email == student.Email && s.Id != id);
                    
                    if (duplicateEmail != null)
                    {
                        ModelState.AddModelError("Email", "អ៊ីមែលនេះត្រូវបានប្រើរួចហើយ");
                        ViewBag.Classes = _context.StudentClasses
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.ClassName)
                            .ToList();
                        return View(student);
                    }

                    // Handle new photo
                    if (Photo != null && Photo.Length > 0)
                    {
                        // Delete old photo
                        if (!string.IsNullOrEmpty(existingStudent.Photo))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + existingStudent.Photo);
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

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photo.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await Photo.CopyToAsync(stream);
                        }

                        student.Photo = "/uploads/students/" + fileName;
                    }
                    else
                    {
                        // Keep existing photo
                        student.Photo = existingStudent.Photo;
                    }

                    // Update class student counts
                    if (existingStudent.ClassId != student.ClassId)
                    {
                        // Decrease old class count
                        if (existingStudent.ClassId.HasValue)
                        {
                            var oldClass = await _context.StudentClasses.FindAsync(existingStudent.ClassId);
                            if (oldClass != null && oldClass.StudentCount > 0)
                            {
                                oldClass.StudentCount--;
                            }
                        }

                        // Increase new class count
                        if (student.ClassId.HasValue)
                        {
                            var newClass = await _context.StudentClasses.FindAsync(student.ClassId);
                            if (newClass != null)
                            {
                                newClass.StudentCount++;
                            }
                        }
                    }

                    // Update properties
                    existingStudent.FullName = student.FullName;
                    existingStudent.Email = student.Email;
                    existingStudent.Phone = student.Phone;
                    existingStudent.Gender = student.Gender;
                    existingStudent.DateOfBirth = student.DateOfBirth;
                    existingStudent.PlaceOfBirth = student.PlaceOfBirth;
                    existingStudent.Major = student.Major;
                    existingStudent.Year = student.Year;
                    existingStudent.ClassId = student.ClassId;
                    existingStudent.Shift = student.Shift;
                    existingStudent.Room = student.Room;
                    existingStudent.Status = student.Status;
                    existingStudent.TuitionFee = student.TuitionFee;
                    existingStudent.PaidAmount = student.PaidAmount;
                    existingStudent.Photo = student.Photo;
                    existingStudent.Notes = student.Notes;
                    existingStudent.IsActive = student.IsActive;
                    existingStudent.UpdatedAt = DateTime.Now;
                    existingStudent.UpdatedBy = GetCurrentUserId();

                    await _context.SaveChangesAsync();

                    await _activityLogService.LogAsync(
                        GetCurrentUserId(),
                        "Student",
                        "Update",
                        $"កែប្រែព័ត៌មានសិស្ស: {student.FullName}"
                    );

                    TempData["Success"] = "កែប្រែព័ត៌មានសិស្សបានជោគជ័យ!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Classes = _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការកែប្រែសិស្ស: " + ex.Message;
                ViewBag.Classes = _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();
                return View(student);
            }
        }

        // GET: /StudentNew/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Class)
                    .Include(s => s.Grades.OrderByDescending(g => g.RecordedAt))
                    .Include(s => s.Attendances.OrderByDescending(a => a.Date))
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    return NotFound();
                }

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student details {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការផ្ទុកព័ត៌មានលម្អិត";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /StudentNew/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound();
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

                // Update class student count
                if (student.ClassId.HasValue)
                {
                    var studentClass = await _context.StudentClasses.FindAsync(student.ClassId);
                    if (studentClass != null && studentClass.StudentCount > 0)
                    {
                        studentClass.StudentCount--;
                    }
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    GetCurrentUserId(),
                    "Student",
                    "Delete",
                    $"លុបសិស្ស: {student.FullName}"
                );

                TempData["Success"] = "លុបសិស្សបានជោគជ័យ!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", id);
                TempData["Error"] = "មានបញ្ហាក្នុងការលុបសិស្ស: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // API Methods
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.Class)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.FullName,
                        s.Email,
                        s.Phone,
                        s.Gender,
                        s.Major,
                        s.Year,
                        s.ClassId,
                        ClassName = s.Class != null ? s.Class.ClassName : "",
                        s.Shift,
                        s.Room,
                        s.Status,
                        s.TuitionFee,
                        s.PaidAmount,
                        OutstandingBalance = s.TuitionFee - s.PaidAmount,
                        s.Photo,
                        s.CreatedAt,
                        s.IsActive,
                        StudentId = "STU" + s.Id.ToString("D6")
                    })
                    .ToListAsync();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students API");
                return Json(new { success = false, message = ex.Message });
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
                var active = await _context.Students.CountAsync(s => s.IsActive);

                return Json(new { total, male, female, active });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _context.StudentClasses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .Select(c => new
                    {
                        c.Id,
                        c.ClassName,
                        c.Major,
                        c.Year,
                        c.StudentCount,
                        c.MaxStudents
                    })
                    .ToListAsync();

                return Json(new { success = true, data = classes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting classes API");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }
    }
}
