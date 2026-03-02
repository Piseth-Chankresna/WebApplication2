using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class StudentController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // 1. Student List Page
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        // 2. Check Attendance Page (Matches your View)
        public async Task<IActionResult> CheckAttendance()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        // 3. Attendance List Page (Matches your View)
        public async Task<IActionResult> AttendanceList()
        {
            var students = await _context.Students.ToListAsync();
            return View(students);
        }

        // 4. API: Get Students for JSON/AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            return Json(await _context.Students.ToListAsync());
        }

        // 5. API: Save Student to SQL (Used by Create.cshtml)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Student student)
        {
            if (student == null) return BadRequest();

            // Set ID from Timestamp if empty (matching your JS logic)
            if (string.IsNullOrEmpty(student.Id))
                student.Id = DateTime.Now.Ticks.ToString();

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Saved to SQL!" });
        }
    }
}