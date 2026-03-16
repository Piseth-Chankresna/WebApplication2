using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class UserController(ApplicationDbContext context, ActivityLogService activityLogService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ActivityLogService _activityLogService = activityLogService;

        // ==================== USER ACCOUNT DASHBOARD ====================
        [Authorize(Roles = "Admin,User")]
        public IActionResult UserAccount()
        {
            return View();
        }

        // ==================== USER LIST - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.UserAccounts.ToListAsync();
            return View(users);
        }

        // ==================== EXPORT TO EXCEL - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToExcel()
        {
            var users = await _context.UserAccounts.ToListAsync();

            // បង្កើត DataTable
            System.Data.DataTable dt = new();
            dt.Columns.Add("ID");
            dt.Columns.Add("ឈ្មោះពេញ");
            dt.Columns.Add("អ៊ីមែល");
            dt.Columns.Add("តួនាទី");
            dt.Columns.Add("ស្ថានភាព");
            dt.Columns.Add("ថ្ងៃបង្កើត");

            foreach (var user in users)
            {
                dt.Rows.Add(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.IsActive ? "សកម្ម" : "អសកម្ម",
                    user.CreatedAt.ToString("dd/MM/yyyy")
                );
            }

            // ប្រើ ClosedXML ដើម្បីបង្កើត Excel
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add(dt, "Users");

            // កំណត់រចនាសម្ព័ន្ធ
            ws.Columns().AdjustToContents();
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4361ee");
            ws.Row(1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "User_List.xlsx");
        }

        // ==================== CREATE USER - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(UserAccount user)
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
                    TempData["Error"] = "សូមបញ្ចូលពាក្យសម្ងាត់";
                    return View(user);
                }

                if (user.Password.Length < 6)
                {
                    TempData["Error"] = "ពាក្យសម្ងាត់ត្រូវមានយ៉ាងហោចណាស់ ៦ តួអក្សរ";
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

                // Log activity
                await _activityLogService.LogAsync(
                    user.Id,
                    user.FullName ?? "Unknown",
                    "បង្កើតអ្នកប្រើប្រាស់",
                    $"បង្កើតអ្នកប្រើប្រាស់ថ្មី: {user.FullName} ({user.Email})"
                );

                TempData["Success"] = "បង្កើតអ្នកប្រើប្រាស់ថ្មីបានជោគជ័យ!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return View(user);
            }
        }

        // ==================== EDIT USER - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

                // Log activity
                await _activityLogService.LogAsync(
                    id,
                    user.FullName ?? "Unknown",
                    "កែប្រែអ្នកប្រើប្រាស់",
                    $"កែប្រែព័ត៌មានអ្នកប្រើប្រាស់: {user.FullName}"
                );

                TempData["Success"] = "កែប្រែព័ត៌មានដោយជោគជ័យ!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "មានបញ្ហា: " + ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // ==================== DELETE USER - ADMIN ONLY ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញអ្នកប្រើប្រាស់!" });
                }

                var userName = user.FullName ?? "Unknown";

                _context.UserAccounts.Remove(user);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    id,
                    userName,
                    "លុបអ្នកប្រើប្រាស់",
                    $"លុបអ្នកប្រើប្រាស់: {userName}"
                );

                return Json(new { success = true, message = "លុបអ្នកប្រើប្រាស់ដោយជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== RESET PASSWORD - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersForReset()
        {
            var users = await _context.UserAccounts
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .ToListAsync();
            return Json(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(int userId, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword))
                {
                    return Json(new { success = false, message = "សូមបញ្ចូលពាក្យសម្ងាត់ថ្មី" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "ពាក្យសម្ងាត់មិនត្រូវគ្នា" });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "ពាក្យសម្ងាត់ត្រូវមានយ៉ាងហោចណាស់ ៦ តួអក្សរ" });
                }

                var user = await _context.UserAccounts.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញអ្នកប្រើប្រាស់" });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    userId,
                    user.FullName ?? "Unknown",
                    "ប្តូរពាក្យសម្ងាត់",
                    $"ប្តូរពាក្យសម្ងាត់សម្រាប់: {user.FullName}"
                );

                return Json(new { success = true, message = "ប្តូរពាក្យសម្ងាត់ជោគជ័យ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "មានបញ្ហា: " + ex.Message });
            }
        }

        // ==================== USER PROFILE ====================
        public async Task<IActionResult> Profile(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // ==================== CURRENT USER PROFILE ====================
        public async Task<IActionResult> MyProfile()
        {
            // Get current logged-in user ID
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = int.Parse(userIdClaim.Value);
            var user = await _context.UserAccounts.FindAsync(currentUserId);
            
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View("Profile", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] UserAccount updatedUser, IFormFile? ProfileImage)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(updatedUser.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញអ្នកប្រើប្រាស់" });
                }

                user.FullName = updatedUser.FullName;
                user.Email = updatedUser.Email;

                // រក្សាទុករូបភាព (បើមាន)
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    // បង្កើត Folder uploads បើមិនទាន់មាន
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    user.ProfileImage = "/uploads/" + fileName;
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    user.Id,
                    user.FullName ?? "Unknown",
                    "កែប្រែប្រវត្តិរូប",
                    $"កែប្រែប្រវត្តិរូបអ្នកប្រើប្រាស់: {user.FullName}"
                );

                return Json(new { success = true, message = "កែប្រែប្រវត្តិរូបដោយជោគជ័យ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== UPDATE CURRENT USER PROFILE ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCurrentUserProfile([FromForm] ProfileUpdateModel updatedUser, IFormFile? ProfileImage)
        {
            try
            {
                // Check model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Model validation failed: " + string.Join(", ", errors) });
                }

                // Get current logged-in user ID
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null)
                {
                    return Json(new { success = false, message = "មិនអាចរកឃើញព័ត៌មានអ្នកប្រើប្រាស់" });
                }

                var currentUserId = int.Parse(userIdClaim.Value);
                var user = await _context.UserAccounts.FindAsync(currentUserId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "រកមិនឃើញអ្នកប្រើប្រាស់" });
                }

                // Update user info
                user.FullName = updatedUser.FullName;
                user.Email = updatedUser.Email;

                // Handle profile image upload
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    // Delete old profile image if exists
                    if (!string.IsNullOrEmpty(user.ProfileImage))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    user.ProfileImage = "/uploads/" + fileName;
                }

                await _context.SaveChangesAsync();

                // Re-issue authentication cookie with updated claims
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.FullName ?? ""),
                    new(ClaimTypes.Email, user.Email ?? ""),
                    new("UserId", user.Id.ToString()),
                    new("ProfileImage", user.ProfileImage ?? "")
                };

                // Add role permissions
                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleName == user.Role)
                    .ToListAsync();

                foreach (var perm in permissions)
                {
                    if (!string.IsNullOrEmpty(perm.ModuleName))
                    {
                        claims.Add(new Claim($"Permission_{perm.ModuleName}_View", perm.CanView.ToString()));
                        claims.Add(new Claim($"Permission_{perm.ModuleName}_Create", perm.CanCreate.ToString()));
                        claims.Add(new Claim($"Permission_{perm.ModuleName}_Edit", perm.CanEdit.ToString()));
                        claims.Add(new Claim($"Permission_{perm.ModuleName}_Delete", perm.CanDelete.ToString())); 
                        claims.Add(new Claim($"Permission_{perm.ModuleName}_Export", perm.CanExport.ToString()));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30) // 30 days
                };

                // Sign out and sign in with new claims
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Log activity
                await _activityLogService.LogAsync(
                    user.Id,
                    user.FullName ?? "Unknown",
                    "កែប្រែប្រវត្តិរូប",
                    $"អ្នកប្រើប្រាស់កែប្រែប្រវត្តិរូបផ្ទាល់ខ្លួន"
                );

                return Json(new { 
                    success = true, 
                    message = "កែប្រែប្រវត្តិរូបដោយជោគជ័យ",
                    profileImage = user.ProfileImage,
                    fullName = user.FullName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== ROLES & PERMISSIONS - ADMIN ONLY ====================
        [Authorize(Roles = "Admin")]
        public IActionResult Roles()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPermissions(string roleName)
        {
            try
            {
                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleName == roleName)
                    .ToListAsync();

                if (permissions == null || permissions.Count == 0)
                {
                    var modules = new List<string> {
                        "User Management",
                        "Student Records",
                        "Financial Reports",
                        "Attendance",
                        "Grades",
                        "Classes",
                        "Settings",
                        "Payments",
                        "Scholarships"
                    };

                    permissions = [.. modules.Select(m => new RolePermission
                    {
                        RoleName = roleName,
                        ModuleName = m,
                        CanView = roleName == "Admin",
                        CanCreate = roleName == "Admin",
                        CanEdit = roleName == "Admin",
                        CanDelete = roleName == "Admin",
                        CanExport = roleName == "Admin"
                    })];
                }

                return Json(permissions);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SavePermissions(SavePermissionsModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.RoleName) || model.Permissions == null)
                {
                    return Json(new { success = false, message = "ទិន្នន័យមិនត្រឹមត្រូវ" });
                }

                // Delete old permissions
                var oldPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleName == model.RoleName)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(oldPermissions);

                // Add new permissions
                foreach (var perm in model.Permissions)
                {
                    perm.RoleName = model.RoleName;
                    await _context.RolePermissions.AddAsync(perm);
                }

                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogService.LogAsync(
                    0,
                    "System",
                    "កែប្រែសិទ្ធិ",
                    $"កែប្រែសិទ្ធិសម្រាប់តួនាទី: {model.RoleName}"
                );

                return Json(new { success = true, message = "រក្សាទុកសិទ្ធិរួចរាល់!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== GET USER BY ID ====================
        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Json(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive
            });
        }
    }

    public class SavePermissionsModel
    {
        public string? RoleName { get; set; }
        public List<RolePermission>? Permissions { get; set; }
    }
}
