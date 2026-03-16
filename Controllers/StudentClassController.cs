using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class StudentClassController(ApplicationDbContext context, ActivityLogService activityLog) : BaseController
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLog = activityLog;

        // GET: StudentClass
        public async Task<IActionResult> Index()
        {
            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "View"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var classes = await _context.StudentClasses
                .Include(sc => sc.Students)
                .OrderByDescending(sc => sc.AcademicYear)
                .ThenBy(sc => sc.ClassCode)
                .ToListAsync();

            return View(classes);
        }

        // GET: StudentClass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "View"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var studentClass = await _context.StudentClasses
                .Include(sc => sc.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (studentClass == null)
            {
                return NotFound();
            }

            return View(studentClass);
        }

        // GET: StudentClass/Create
        public IActionResult Create()
        {
            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Create"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        // POST: StudentClass/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClassCode,ClassName,Major,Year,Semester,AcademicYear,MaxStudents,IsActive")] StudentClass studentClass)
        {
            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Create"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    studentClass.CreatedAt = DateTime.Now;
                    studentClass.CreatedBy = PermissionService.GetCurrentUserId(User);
                    studentClass.CurrentStudents = 0;

                    _context.Add(studentClass);
                    await _context.SaveChangesAsync();

                    // Log activity
                    await _activityLog.LogAsync(
                        PermissionService.GetCurrentUserId(User) ?? 0,
                        User.Identity?.Name ?? "System",
                        "Create Class",
                        $"Created new class: {studentClass.ClassCode} - {studentClass.ClassName}"
                    );

                    TempData["SuccessMessage"] = "ថ្នាក់ត្រូវបានបង្កើតដោយជោគជ័យ!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "មានបញ្ហាក្នុងការបង្កើតថ្នាក់: " + ex.Message);
                }
            }

            return View(studentClass);
        }

        // GET: StudentClass/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Edit"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var studentClass = await _context.StudentClasses.FindAsync(id);
            if (studentClass == null)
            {
                return NotFound();
            }

            return View(studentClass);
        }

        // POST: StudentClass/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassCode,ClassName,Major,Year,Semester,AcademicYear,MaxStudents,IsActive,CreatedAt,CreatedBy")] StudentClass studentClass)
        {
            if (id != studentClass.Id)
            {
                return NotFound();
            }

            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Edit"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update current student count
                    studentClass.CurrentStudents = await _context.Students
                        .CountAsync(s => s.ClassId == id && s.Status == "Active");

                    _context.Update(studentClass);
                    await _context.SaveChangesAsync();

                    // Log activity
                    await _activityLog.LogAsync(
                        PermissionService.GetCurrentUserId(User) ?? 0,
                        User.Identity?.Name ?? "System",
                        "Edit Class",
                        $"Updated class: {studentClass.ClassCode} - {studentClass.ClassName}"
                    );

                    TempData["SuccessMessage"] = "ថ្នាក់ត្រូវបានកែប្រែដោយជោគជ័យ!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentClassExists(studentClass.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "មានបញ្ហាក្នុងការកែប្រែថ្នាក់: " + ex.Message);
                }
            }

            return View(studentClass);
        }

        // GET: StudentClass/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Delete"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var studentClass = await _context.StudentClasses
                .Include(sc => sc.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (studentClass == null)
            {
                return NotFound();
            }

            // Check if class has students
            if (studentClass.Students != null && studentClass.Students.Count > 0)
            {
                ModelState.AddModelError("", "មិនអាចលុបថ្នាក់នេះបានទេ ព្រោះមានសិស្សចុះឈ្មោះក្នុងថ្នាក់នេះ!");
                return RedirectToAction(nameof(Index));
            }

            return View(studentClass);
        }

        // POST: StudentClass/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "Delete"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var studentClass = await _context.StudentClasses.FindAsync(id);
            if (studentClass != null)
            {
                try
                {
                    // Check if class has students before deleting
                    var studentCount = await _context.Students.CountAsync(s => s.ClassId == id);
                    if (studentCount > 0)
                    {
                        ModelState.AddModelError("", "មិនអាចលុបថ្នាក់នេះបានទេ ព្រោះមានសិស្សចុះឈ្មោះក្នុងថ្នាក់នេះ!");
                        return RedirectToAction(nameof(Index));
                    }

                    _context.StudentClasses.Remove(studentClass);
                    await _context.SaveChangesAsync();

                    // Log activity
                    await _activityLog.LogAsync(
                        PermissionService.GetCurrentUserId(User) ?? 0,
                        User.Identity?.Name ?? "System",
                        "Delete Class",
                        $"Deleted class: {studentClass.ClassCode} - {studentClass.ClassName}"
                    );

                    TempData["SuccessMessage"] = "ថ្នាក់ត្រូវបានលុបដោយជោគជ័យ!";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "មានបញ្ហាក្នុងការលុបថ្នាក់: " + ex.Message);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: StudentClass/Students/5
        public async Task<IActionResult> Students(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check permission
            if (!PermissionService.HasPermission(User, "StudentClass", "View"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var studentClass = await _context.StudentClasses
                .Include(sc => sc.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (studentClass == null)
            {
                return NotFound();
            }

            ViewBag.ClassInfo = studentClass;
            var students = studentClass.Students?.OrderBy(s => s.Name).ToList() ?? [];
            return View(students);
        }

        private bool StudentClassExists(int id)
        {
            return _context.StudentClasses.Any(e => e.Id == id);
        }

        // ==================== API METHODS FOR AJAX ====================
        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _context.StudentClasses
                    .Select(sc => new
                    {
                        sc.Id,
                        sc.ClassCode,
                        sc.ClassName,
                        sc.Major,
                        sc.Year,
                        sc.Semester,
                        sc.AcademicYear,
                        sc.MaxStudents,
                        sc.StudentCount,
                        sc.Room,
                        sc.TeacherName,
                        sc.TeacherPhoto,
                        sc.Time,
                        sc.IsActive,
                        sc.CreatedAt,
                        CurrentStudents = sc.StudentCount,
                        AvailableSlots = sc.MaxStudents - sc.StudentCount,
                        EnrollmentPercentage = sc.MaxStudents > 0 ? (decimal)sc.StudentCount / sc.MaxStudents * 100 : 0
                    })
                    .OrderByDescending(sc => sc.AcademicYear)
                    .ThenBy(sc => sc.ClassCode)
                    .ToListAsync();

                return Json(new { success = true, data = classes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] StudentClass studentClass)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Check if class code already exists
                var existingClass = await _context.StudentClasses
                    .FirstOrDefaultAsync(sc => sc.ClassCode == studentClass.ClassCode);

                if (existingClass != null)
                {
                    return Json(new { success = false, message = "លេខកូដថ្នាក់នេះមានរួចហើយ!" });
                }

                studentClass.CreatedAt = DateTime.Now;
                studentClass.CreatedBy = PermissionService.GetCurrentUserId(User);
                studentClass.StudentCount = 0;

                _context.Add(studentClass);
                await _context.SaveChangesAsync();

                await _activityLog.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "Create Class",
                    $"Created new class: {studentClass.ClassCode} - {studentClass.ClassName}"
                );

                return Json(new { success = true, message = "ថ្នាក់ត្រូវបានបង្កើតដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateClass([FromBody] StudentClass studentClass)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var existingClass = await _context.StudentClasses.FindAsync(studentClass.Id);
                if (existingClass == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញថ្នាក់នេះទេ!" });
                }

                // Check if class code conflicts with another class
                var conflictClass = await _context.StudentClasses
                    .FirstOrDefaultAsync(sc => sc.ClassCode == studentClass.ClassCode && sc.Id != studentClass.Id);

                if (conflictClass != null)
                {
                    return Json(new { success = false, message = "លេខកូដថ្នាក់នេះមានរួចហើយ!" });
                }

                existingClass.ClassCode = studentClass.ClassCode;
                existingClass.ClassName = studentClass.ClassName;
                existingClass.Major = studentClass.Major;
                existingClass.Year = studentClass.Year;
                existingClass.Semester = studentClass.Semester;
                existingClass.AcademicYear = studentClass.AcademicYear;
                existingClass.MaxStudents = studentClass.MaxStudents;
                existingClass.Room = studentClass.Room;
                existingClass.TeacherName = studentClass.TeacherName;
                existingClass.TeacherPhoto = studentClass.TeacherPhoto;
                existingClass.Time = studentClass.Time;

                await _context.SaveChangesAsync();

                await _activityLog.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "Edit Class",
                    $"Updated class: {existingClass.ClassCode} - {existingClass.ClassName}"
                );

                return Json(new { success = true, message = "ថ្នាក់ត្រូវបានកែប្រែដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                var studentClass = await _context.StudentClasses
                    .Include(sc => sc.Students)
                    .FirstOrDefaultAsync(sc => sc.Id == id);

                if (studentClass == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញថ្នាក់នេះទេ!" });
                }

                // Check if class has students
                if (studentClass.Students != null && studentClass.Students.Count > 0)
                {
                    return Json(new { success = false, message = "មិនអាចលុបថ្នាក់នេះបានទេ ព្រោះមានសិស្សចុះឈ្មោះក្នុងថ្នាក់នេះ!" });
                }

                _context.StudentClasses.Remove(studentClass);
                await _context.SaveChangesAsync();

                await _activityLog.LogAsync(
                    PermissionService.GetCurrentUserId(User) ?? 0,
                    User.Identity?.Name ?? "System",
                    "Delete Class",
                    $"Deleted class: {studentClass.ClassCode} - {studentClass.ClassName}"
                );

                return Json(new { success = true, message = "ថ្នាក់ត្រូវបានលុបដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
