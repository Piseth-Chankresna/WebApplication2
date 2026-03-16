using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public partial class AccountController(ApplicationDbContext context, ActivityLogService activityLog) : Controller
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ActivityLogService _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 15;

        // Use generated regex for better performance
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();

        // Role Selection Page
        [HttpGet]
        public IActionResult RoleSelection()
        {
            // If already logged in, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (User.IsInRole("User"))
                {
                    return RedirectToAction("UserAccount", "User");
                }
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("User"))
                {
                    return RedirectToAction("UserAccount", "User");
                }
                return RedirectToAction("Index", "Home");
            }

            // If there's a returnUrl and it's local, go to login page directly
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // Otherwise redirect to role selection page
            return RedirectToAction("RoleSelection");
        }

        [HttpGet]
        public IActionResult AdminLogin(string? returnUrl = null)
        {
            // If already logged in, redirect to home page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Store role in ViewBag for the login page
            ViewBag.Role = "Admin";
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }

        [HttpGet]
        public IActionResult UserLogin(string? returnUrl = null)
        {
            // If already logged in, redirect to home page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Store role in ViewBag for the login page
            ViewBag.Role = "User";
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find user by email
                var user = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                // Check if user exists
                if (user == null)
                {
                    ModelState.AddModelError("", "អ៊ីមែល ឬ លេខសម្ងាត់មិនត្រឹមត្រូវ");
                    await _activityLog.LogAsync(0, model.Email ?? "Unknown", "Login Failed",
                        $"Login attempt with non-existent email: {model.Email}", "បរាជ័យ");
                    return View(model);
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "គណនីរបស់អ្នកត្រូវបានបិទ។ សូមទាក់ទងអ្នកគ្រប់គ្រង");
                    await _activityLog.LogAsync(user.Id, user.FullName ?? "Unknown", "Login Failed",
                        "Login attempt on inactive account", "បរាជ័យ");
                    return View(model);
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "អ៊ីមែល ឬ លេខសម្ងាត់មិនត្រឹមត្រូវ");
                    await _activityLog.LogAsync(user.Id, user.FullName ?? "Unknown", "Login Failed",
                        "Invalid password attempt", "បរាជ័យ");
                    return View(model);
                }

                // Get user permissions
                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleName == user.Role)
                    .ToListAsync();

                // Create claims
                var claims = new List<Claim>();

                if (!string.IsNullOrEmpty(user.FullName))
                    claims.Add(new Claim(ClaimTypes.Name, user.FullName));

                if (!string.IsNullOrEmpty(user.Email))
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));

                if (!string.IsNullOrEmpty(user.Role))
                    claims.Add(new Claim(ClaimTypes.Role, user.Role));

                claims.Add(new Claim("UserId", user.Id.ToString()));

                if (!string.IsNullOrEmpty(user.ProfileImage))
                    claims.Add(new Claim("ProfileImage", user.ProfileImage));

                // Add permissions as claims
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

                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Set authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Log successful login
                await _activityLog.LogAsync(user.Id, user.FullName ?? "Unknown", "Login",
                    "User logged in successfully", "ជោគជ័យ");

                // ========== REDIRECT TO HOME/INDEX AFTER SUCCESSFUL LOGIN ==========
                // If there's a returnUrl and it's a local URL, redirect to that
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Otherwise redirect to Home/Index
                if (user.Role == "User")
                {
                    return RedirectToAction("UserAccount", "User");
                }
                return RedirectToAction("Index", "Home");
                // ====================================================================
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "មានបញ្ហាក្នុងការចូលប្រើប្រាស់។ សូមព្យាយាមម្តងទៀត");
                // Log error
                Console.WriteLine($"Login error: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            // If already logged in, redirect to home page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Check if coming from role-specific login
            var role = HttpContext.Request.Query["role"];
            if (!string.IsNullOrEmpty(role))
            {
                ViewBag.Role = role;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserAccount model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Validate email format - using generated regex
                if (!IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "ទម្រង់អ៊ីមែលមិនត្រឹមត្រូវ");
                    return View(model);
                }

                // Check if email already exists
                var existingUser = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "អ៊ីមែលនេះមានរួចហើយ។ សូមប្រើអ៊ីមែលផ្សេង");
                    return View(model);
                }

                // Validate password strength
                var (IsValid, Message) = ValidatePasswordStrength(model.Password);
                if (!IsValid)
                {
                    ModelState.AddModelError("Password", Message);
                    return View(model);
                }

                // Validate password confirmation
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "លេខសម្ងាត់មិនត្រូវគ្នា");
                    return View(model);
                }

                // Hash password
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                
                // Set role from ViewBag if available (from role-specific login), otherwise use form value
                if (!string.IsNullOrEmpty(ViewBag.Role as string))
                {
                    model.Role = ViewBag.Role as string;
                }

                // Add user to database
                _context.UserAccounts.Add(model);
                await _context.SaveChangesAsync();

                // Log registration
                await _activityLog.LogAsync(model.Id, model.FullName ?? "Unknown", "Register",
                    "New user registered successfully", "ជោគជ័យ");

                TempData["SuccessMessage"] = "ការចុះឈ្មោះបានជោគជ័យ! សូមចូលប្រើប្រាស់";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "មានបញ្ហាក្នុងការចុះឈ្មោះ។ សូមព្យាយាមម្តងទៀត");
                Console.WriteLine($"Registration error: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get user info before logout
                var userId = User.FindFirst("UserId")?.Value;
                var userName = User.Identity?.Name ?? "Unknown";

                // Sign out
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Log logout
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
                {
                    await _activityLog.LogAsync(userIdInt, userName, "Logout",
                        "User logged out successfully", "ជោគជ័យ");
                }

                TempData["InfoMessage"] = "អ្នកបានចាកចេញដោយជោគជ័យ";
                
                // Redirect to login page after logout
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Helper method to validate email format - using generated regex
        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return EmailRegex().IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Helper method to validate password strength
        private static (bool IsValid, string Message) ValidatePasswordStrength(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "សូមបញ្ចូលលេខសម្ងាត់");

            if (password.Length < 8)
                return (false, "លេខសម្ងាត់ត្រូវមានយ៉ាងហោចណាស់ 8 តួអក្សរ");

            // Check for at least one uppercase letter
            if (!password.Any(char.IsUpper))
                return (false, "លេខសម្ងាត់ត្រូវមានអក្សរធំយ៉ាងហោចណាស់ 1 តួ");

            // Check for at least one lowercase letter
            if (!password.Any(char.IsLower))
                return (false, "លេខសម្ងាត់ត្រូវមានអក្សរតូចយ៉ាងហោចណាស់ 1 តួ");

            // Check for at least one digit
            if (!password.Any(char.IsDigit))
                return (false, "លេខសម្ងាត់ត្រូវមានលេខយ៉ាងហោចណាស់ 1 តួ");

            return (true, string.Empty);
        }
    }
}