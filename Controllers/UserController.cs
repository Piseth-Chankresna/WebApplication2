using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebApplication2.Controllers
{
    public class UserController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // ==================== USER LIST ====================
        public async Task<IActionResult> Index()
        {
            var users = await _context.UserAccounts.ToListAsync();
            return View(users);
        }

        // ==================== CREATE USER ====================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAccount user, string ConfirmPassword)
        {
            try
            {
                // Manual validation
                if (string.IsNullOrEmpty(user.FullName))
                {
                    TempData["Error"] = "សូមបញ្ចូលឈ្មោះពេញ";
                    return View(user);
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    TempData["Error"] = "សូមបញ្ចូលអ៊ីមែល";
                    return View(user);
                }

                if (string.IsNullOrEmpty(user.Role))
                {
                    TempData["Error"] = "សូមជ្រើសរើសតួនាទី";
                    return View(user);
                }

                if (string.IsNullOrEmpty(user.Password))
                {
                    TempData["Error"] = "សូមបញ្ចូលលេខសម្ងាត់";
                    return View(user);
                }

                if (user.Password.Length < 6)
                {
                    TempData["Error"] = "លេខសម្ងាត់ត្រូវមានយ៉ាងហោចណាស់ ៦ តួអក្សរ";
                    return View(user);
                }

                // Check if passwords match
                if (user.Password != ConfirmPassword)
                {
                    TempData["Error"] = "លេខសម្ងាត់មិនត្រូវគ្នា";
                    return View(user);
                }

                // Check if email exists
                var existingUser = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (existingUser != null)
                {
                    TempData["Error"] = "អ៊ីមែលនេះមានរួចហើយ!";
                    return View(user);
                }

                // Hash password
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "បង្កើតអ្នកប្រើប្រាស់ថ្មីបានជោគជ័យ!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return View(user);
            }
        }

        // ==================== EDIT USER ====================
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserAccount updatedUser)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = updatedUser.FullName;
                user.Email = updatedUser.Email;
                user.Role = updatedUser.Role;
                user.IsActive = updatedUser.IsActive;

                await _context.SaveChangesAsync();

                TempData["Success"] = "កែប្រែព័ត៌មានដោយជោគជ័យ!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return RedirectToAction("Edit", new { id });
            }
        }

        // ==================== DELETE USER ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញអ្នកប្រើប្រាស់!" });
                }

                _context.UserAccounts.Remove(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "លុបអ្នកប្រើប្រាស់ដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== RESET PASSWORD ====================
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword))
                {
                    TempData["Error"] = "សូមបញ្ចូលលេខសម្ងាត់ថ្មី";
                    return RedirectToAction("ResetPassword", new { id });
                }

                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "លេខសម្ងាត់មិនត្រូវគ្នា";
                    return RedirectToAction("ResetPassword", new { id });
                }

                if (newPassword.Length < 6)
                {
                    TempData["Error"] = "លេខសម្ងាត់ត្រូវមានយ៉ាងហោចណាស់ ៦ តួអក្សរ";
                    return RedirectToAction("ResetPassword", new { id });
                }

                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                TempData["Success"] = "ប្តូរពាក្យសម្ងាត់ជោគជ័យ!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return RedirectToAction("ResetPassword", new { id });
            }
        }

        // ==================== ROLES & PERMISSIONS ====================
        public IActionResult Roles()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissions(string roleName)
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleName == roleName)
                .ToListAsync();
            return Json(permissions);
        }
    }
}