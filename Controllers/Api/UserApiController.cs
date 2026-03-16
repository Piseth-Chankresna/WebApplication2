using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Simple API working!", timestamp = DateTime.Now });
        }

        [HttpGet("students")]
        public IActionResult GetStudents()
        {
            var students = new[]
            {
                new { id = "1", name = "Test Student 1", status = "Active" },
                new { id = "2", name = "Test Student 2", status = "Active" }
            };
            return Ok(students);
        }
    }

    [Route("api/User")]
    [ApiController]
    [Authorize]
    public class UserApiController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        // Test endpoint - no authentication required
        [HttpGet("Test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            try
            {
                return Ok(new { message = "API is working!", timestamp = DateTime.Now, status = "success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Test failed", message = ex.Message });
            }
        }

        // GET: api/User/GetStudents
        [HttpGet("GetStudents")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.UserAccounts
                    .Where(u => u.Role == "Student")
                    .Select(s => new
                    {
                        id = s.Id,
                        fullName = s.FullName,
                        email = s.Email,
                        phone = s.Phone ?? "N/A",
                        className = s.ClassName ?? "N/A",
                        status = s.IsActive ? "Active" : "Inactive",
                        createdAt = s.CreatedAt,
                        role = s.Role,
                        major = s.Major ?? "N/A",
                        year = s.Year ?? "N/A",
                        gender = s.Gender ?? "N/A"
                    })
                    .OrderByDescending(s => s.createdAt)
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching students", message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/User/GetClasses
        [HttpGet("GetClasses")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                // Get actual classes from database
                var classes = await _context.StudentClasses
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.ClassName ?? "Unknown Class",
                        major = c.Major ?? "N/A",
                        year = c.Year ?? "N/A",
                        studentCount = c.StudentCount,
                        maxStudents = c.MaxStudents,
                        teacher = c.TeacherName ?? "N/A",
                        academicYear = c.AcademicYear ?? "N/A",
                        createdAt = c.CreatedAt
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching classes", message = ex.Message });
            }
        }

        // GET: api/User/GetGrades
        [HttpGet("GetGrades")]
        public async Task<IActionResult> GetGrades()
        {
            try
            {
                // Get actual grades from database
                var grades = await _context.Grades
                    .Join(_context.Students, 
                        grade => grade.StudentId, 
                        student => student.Id, 
                        (grade, student) => new { grade, student })
                    .Join(_context.StudentClasses,
                        combined => combined.student.ClassId,
                        studentClass => studentClass.Id,
                        (combined, studentClass) => new { combined.grade, combined.student, studentClass })
                    .Select(g => new
                    {
                        id = g.grade.Id,
                        studentName = g.student.Name,
                        className = g.studentClass.ClassName,
                        subject = g.grade.Subject,
                        attendance = g.grade.Attendance,
                        assignment = g.grade.Assignment,
                        midTerm = g.grade.MidTerm,
                        finalExam = g.grade.FinalExam,
                        total = g.grade.Total,
                        gradeLetter = g.grade.GradeLetter,
                        semester = g.grade.Semester,
                        academicYear = g.grade.AcademicYear,
                        recordedAt = g.grade.RecordedAt
                    })
                    .OrderByDescending(g => g.recordedAt)
                    .ToListAsync();

                return Ok(grades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching grades", message = ex.Message });
            }
        }

        // GET: api/User/GetAttendance
        [HttpGet("GetAttendance")]
        public async Task<IActionResult> GetAttendance()
        {
            try
            {
                // Get actual attendance from database
                var attendance = await _context.Attendances
                    .Join(_context.Students,
                        attendance => attendance.StudentId,
                        student => student.Id,
                        (attendance, student) => new { attendance, student })
                    .Join(_context.StudentClasses,
                        combined => combined.student.ClassId,
                        studentClass => studentClass.Id,
                        (combined, studentClass) => new { combined.attendance, combined.student, studentClass })
                    .Select(a => new
                    {
                        id = a.attendance.Id,
                        attendanceCode = a.attendance.AttendanceCode,
                        studentName = a.attendance.StudentName,
                        className = a.studentClass.ClassName,
                        date = a.attendance.Date.ToString("yyyy-MM-dd"),
                        status = a.attendance.Status,
                        room = a.attendance.Room,
                        note = a.attendance.Note,
                        recordedAt = a.attendance.RecordedAt
                    })
                    .OrderByDescending(a => a.date)
                    .ToListAsync();

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching attendance", message = ex.Message });
            }
        }

        // GET: api/User/GetStudentById/{id}
        [HttpGet("GetStudentById/{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            try
            {
                var student = await _context.UserAccounts
                    .Where(u => u.Id == id)
                    .Select(s => new
                    {
                        id = s.Id,
                        fullName = s.FullName,
                        email = s.Email,
                        phone = s.Phone ?? "N/A",
                        className = s.ClassName ?? "N/A",
                        status = s.IsActive ? "Active" : "Inactive",
                        role = s.Role,
                        createdAt = s.CreatedAt,
                        profileImage = s.ProfileImage
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return NotFound(new { error = "Student not found" });
                }

                return Ok(student);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error fetching student", message = ex.Message });
            }
        }

        // POST: api/User/CreateStudent (Permission check - will return 403 for users)
        [HttpPost("CreateStudent")]
        public async Task<IActionResult> CreateStudent([FromBody] UserAccount student)
        {
            // Check if user is Admin
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to create students");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Check for duplicate email
                    var existingUser = await _context.UserAccounts
                        .FirstOrDefaultAsync(u => u.Email == student.Email && u.Id != student.Id);
                    
                    if (existingUser != null)
                    {
                        return BadRequest(new { error = "Email already exists" });
                    }

                    // Set default values for new student
                    student.Role = "Student";
                    student.CreatedAt = DateTime.Now;
                    student.IsActive = true;
                    
                    _context.UserAccounts.Add(student);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Student created successfully" });
                }
                return BadRequest(new { error = "Invalid student data", details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error creating student", message = ex.Message });
            }
        }

        // PUT: api/User/UpdateStudent/{id} (Permission check - will return 403 for users)
        [HttpPut("UpdateStudent/{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UserAccount student)
        {
            // Check if user is Admin
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to update students");
            }

            try
            {
                var existingStudent = await _context.UserAccounts.FindAsync(id);
                if (existingStudent == null)
                {
                    return NotFound(new { error = "Student not found" });
                }

                // Update properties
                existingStudent.FullName = student.FullName;
                existingStudent.Email = student.Email;
                existingStudent.Phone = student.Phone;
                existingStudent.ClassName = student.ClassName;
                existingStudent.Major = student.Major;
                existingStudent.Year = student.Year;
                existingStudent.Gender = student.Gender;
                existingStudent.IsActive = student.IsActive;
                
                // Update password if provided
                if (!string.IsNullOrEmpty(student.Password))
                {
                    existingStudent.Password = student.Password;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Student updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating student", message = ex.Message });
            }
        }

        // POST: api/User/CreateClass
        [HttpPost("CreateClass")]
        public async Task<IActionResult> CreateClass([FromBody] StudentClass studentClass)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to create classes");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    studentClass.CreatedAt = DateTime.Now;
                    studentClass.IsActive = true;
                    studentClass.StudentCount = 0;
                    
                    _context.StudentClasses.Add(studentClass);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Class created successfully", data = studentClass });
                }
                return BadRequest(new { error = "Invalid class data", details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error creating class", message = ex.Message });
            }
        }

        // PUT: api/User/UpdateClass/{id}
        [HttpPut("UpdateClass/{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] StudentClass studentClass)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to update classes");
            }

            try
            {
                var existingClass = await _context.StudentClasses.FindAsync(id);
                if (existingClass == null)
                {
                    return NotFound(new { error = "Class not found" });
                }

                // Update properties
                existingClass.ClassName = studentClass.ClassName;
                existingClass.Major = studentClass.Major;
                existingClass.Year = studentClass.Year;
                existingClass.TeacherName = studentClass.TeacherName;
                existingClass.TeacherPhoto = studentClass.TeacherPhoto;
                existingClass.Time = studentClass.Time;
                existingClass.Room = studentClass.Room;
                existingClass.AcademicYear = studentClass.AcademicYear;
                existingClass.MaxStudents = studentClass.MaxStudents;
                existingClass.IsActive = studentClass.IsActive;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Class updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating class", message = ex.Message });
            }
        }

        // DELETE: api/User/DeleteClass/{id}
        [HttpDelete("DeleteClass/{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to delete classes");
            }

            try
            {
                var studentClass = await _context.StudentClasses.FindAsync(id);
                if (studentClass == null)
                {
                    return NotFound(new { error = "Class not found" });
                }

                // Check if class has students
                var hasStudents = await _context.Students.AnyAsync(s => s.ClassId == id);
                if (hasStudents)
                {
                    return BadRequest(new { error = "Cannot delete class with enrolled students" });
                }

                _context.StudentClasses.Remove(studentClass);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Class deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting class", message = ex.Message });
            }
        }

        // DELETE: api/User/DeleteStudent/{id} (Permission check - will return 403 for users)
        [HttpDelete("DeleteStudent/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            // Check if user is Admin
            if (!User.IsInRole("Admin"))
            {
                return Forbid("User does not have permission to delete students");
            }

            try
            {
                var student = await _context.UserAccounts.FindAsync(id);
                if (student == null)
                {
                    return NotFound(new { error = "Student not found" });
                }

                _context.UserAccounts.Remove(student);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting student", message = ex.Message });
            }
        }
    }
}
