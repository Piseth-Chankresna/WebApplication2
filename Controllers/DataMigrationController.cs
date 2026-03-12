using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataMigrationController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpPost("import-all")]
        public async Task<IActionResult> ImportAllData()
        {
            try
            {
                var results = new
                {
                    Users = await ImportUsers(),
                    Classes = await ImportClasses(),
                    Payments = await ImportPayments()
                };

                return Ok(new
                {
                    success = true,
                    message = "បាននាំចូលទិន្នន័យដោយជោគជ័យ!",
                    results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        private async Task<int> ImportUsers()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "system_database.json");
            if (!System.IO.File.Exists(jsonPath)) return 0;

            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
            var data = JsonSerializer.Deserialize<SystemDataWrapper>(json);

            int imported = 0;

            if (data?.Users != null && data.Users.Count != 0)
            {
                foreach (var user in data.Users)
                {
                    var exists = await _context.UserAccounts.AnyAsync(u => u.Email == user.Email);
                    if (!exists)
                    {
                        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                        user.CreatedAt = DateTime.Now;
                        await _context.UserAccounts.AddAsync(user);
                        imported++;
                    }
                }
                await _context.SaveChangesAsync();
            }

            if (data?.RolePermissions != null)
            {
                foreach (var role in data.RolePermissions)
                {
                    foreach (var perm in role.Value)
                    {
                        var exists = await _context.RolePermissions
                            .AnyAsync(rp => rp.RoleName == role.Key && rp.ModuleName == perm.ModuleName);

                        if (!exists)
                        {
                            perm.RoleName = role.Key;
                            await _context.RolePermissions.AddAsync(perm);
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            return imported;
        }

        private async Task<int> ImportClasses()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "classes.json");
            if (!System.IO.File.Exists(jsonPath)) return 0;

            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
            var classes = JsonSerializer.Deserialize<List<StudentClass>>(json);

            int imported = 0;

            if (classes != null && classes.Count != 0)
            {
                foreach (var cls in classes)
                {
                    var exists = await _context.StudentClasses.AnyAsync(c => c.ClassCode == cls.ClassCode);
                    if (!exists)
                    {
                        await _context.StudentClasses.AddAsync(cls);
                        imported++;
                    }
                }
                await _context.SaveChangesAsync();
            }

            return imported;
        }

        private async Task<int> ImportPayments()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "payments.json");
            if (!System.IO.File.Exists(jsonPath)) return 0;

            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
            var payments = JsonSerializer.Deserialize<List<Payment>>(json);

            int imported = 0;

            if (payments != null && payments.Count != 0)
            {
                foreach (var payment in payments)
                {
                    payment.PaymentDate = DateTime.Now;
                    await _context.Payments.AddAsync(payment);
                    imported++;
                }
                await _context.SaveChangesAsync();
            }

            return imported;
        }

        [HttpPost("import-attendance")]
        public async Task<IActionResult> ImportAttendance([FromBody] List<Attendance> attendances)
        {
            try
            {
                if (attendances != null && attendances.Count != 0)
                {
                    await _context.Attendances.AddRangeAsync(attendances);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, count = attendances.Count });
                }
                return Ok(new { success = true, count = 0 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("import-grades")]
        public async Task<IActionResult> ImportGrades([FromBody] List<Grade> grades)
        {
            try
            {
                if (grades != null && grades.Count != 0)
                {
                    foreach (var grade in grades)
                    {
                        grade.Total = grade.Attendance + grade.Assignment + grade.MidTerm + grade.FinalExam;
                        if (grade.Total >= 90) grade.GradeLetter = "A";
                        else if (grade.Total >= 80) grade.GradeLetter = "B";
                        else if (grade.Total >= 70) grade.GradeLetter = "C";
                        else if (grade.Total >= 60) grade.GradeLetter = "D";
                        else if (grade.Total >= 50) grade.GradeLetter = "E";
                        else grade.GradeLetter = "F";
                    }

                    await _context.Grades.AddRangeAsync(grades);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, count = grades.Count });
                }
                return Ok(new { success = true, count = 0 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}