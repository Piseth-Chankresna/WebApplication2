using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class AccountController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) // Fix possible null
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "អ៊ីមែល ឬ ពាក្យសម្ងាត់មិនត្រឹមត្រូវ");
                return View(model);
            }

            // Get user permissions
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleName == user.Role)
                .ToListAsync();

            var claims = new List<Claim>();

            // Add non-null values with null checks
            if (!string.IsNullOrEmpty(user.FullName))
                claims.Add(new Claim(ClaimTypes.Name, user.FullName));

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            if (!string.IsNullOrEmpty(user.Role))
                claims.Add(new Claim(ClaimTypes.Role, user.Role));

            claims.Add(new Claim("UserId", user.Id.ToString()));

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

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Dashboard", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserAccount user)
        {
            if (ModelState.IsValid)
            {
                // Check if email exists
                var existingUser = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "អ៊ីមែលនេះមានរួចហើយ");
                    return View(user);
                }

                // Hash password
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.IsActive = true;
                user.CreatedAt = DateTime.Now;
                user.Role = "User"; // Default role for new registrations

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "ការចុះឈ្មោះបានជោគជ័យ! សូមចូលប្រើប្រាស់";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}