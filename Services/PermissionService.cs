using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication2.Models;
using WebApplication2.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Services
{
    public static class PermissionService
    {
        // Check if user has specific permission
        public static bool HasPermission(ClaimsPrincipal user, string module, string action)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(role))
                return false;

            // Admin has full access to everything
            if (role == "Admin")
                return true;

            // Check database-based permissions for User role
            if (role == "User")
            {
                // Check claims-based permissions first
                var permissionClaim = user.FindFirst($"Permission_{module}_{action}")?.Value;
                if (permissionClaim != null)
                {
                    return bool.Parse(permissionClaim);
                }

                // Fallback to hardcoded permissions
                return GetUserPermissions(module).Contains(action);
            }

            return false;
        }

        // Enhanced permission checking with database support
        public static async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string module, string action, ApplicationDbContext context)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(role))
                return false;

            // Admin has full access
            if (role == "Admin")
                return true;

            // Check database permissions
            var rolePermission = await context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleName == role && rp.ModuleName == module);

            if (rolePermission != null)
            {
                return action switch
                {
                    "View" or "Index" or "Details" => rolePermission.CanView,
                    "Create" or "Add" => rolePermission.CanCreate,
                    "Edit" or "Update" => rolePermission.CanEdit,
                    "Delete" or "Remove" => rolePermission.CanDelete,
                    "Export" => rolePermission.CanExport,
                    _ => false
                };
            }

            return false;
        }

        // Get permissions for User role per module
        private static List<string> GetUserPermissions(string module)
        {
            var permissions = new Dictionary<string, List<string>>
            {
                // Student Management - Users can view only (no create, edit, delete)
                { "Students", new List<string> { "View", "Details", "Index" } },
                
                // User Management - Users cannot access at all
                { "Users", new List<string>() },
                
                // StudentClass Management - Users can view only
                { "StudentClass", new List<string> { "View", "Details", "Index" } },
                
                // Payments - Users can see and manage payments
                { "Payments", new List<string> { "View", "Create", "Edit", "Details", "Index", "Export" } },
                
                // Attendance - Users can view and manage attendance
                { "Attendance", new List<string> { "View", "Create", "Edit", "Details", "Index" } },
                
                // Grades - Users can view but cannot modify grades
                { "Grades", new List<string> { "View", "Details", "Index" } },
                
                // Reports - Users can view and export reports
                { "Reports", new List<string> { "View", "Export", "Index", "Details" } },
                
                // Settings - Users cannot access settings
                { "Settings", new List<string>() }
            };

            return permissions.ContainsKey(module) ? permissions[module] : new List<string>();
        }

        // Check if user can access controller/action
        public static bool CanAccess(ClaimsPrincipal user, string controllerName, string actionName)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(role))
                return false;

            // Admin can access everything
            if (role == "Admin")
                return true;

            // Enhanced user permissions by controller
            var userControllerPermissions = new Dictionary<string, List<string>>
            {
                // Home - All users can access all Home actions
                { "Home", new List<string> { "Index", "Dashboard", "Privacy", "*" } },
                
                // Student - Users can view only (no create, edit, delete)
                { "Student", new List<string> { "Index", "Details" } },
                
                // User Management - Users cannot access at all
                { "User", new List<string>() },
                
                // StudentClass - Users can view only
                { "StudentClass", new List<string> { "Index", "Details" } },
                
                // Payment - Users can manage payments
                { "Payment", new List<string> { "Index", "Details", "Create", "Edit", "Update", "Export" } },
                
                // Attendance - Users can manage attendance
                { "Attendance", new List<string> { "Index", "Details", "Create", "Edit", "Update" } },
                
                // Grade - Users can only view grades
                { "Grade", new List<string> { "Index", "Details" } },
                
                // Report - Users can view and export reports
                { "Report", new List<string> { "Index", "Details", "Export" } },
                
                // Account - Users can manage their own account
                { "Account", new List<string> { "Index", "Profile", "Logout", "AccessDenied", "Login", "Register", "RoleSelection" } },
                
                // Settings - Users cannot access
                { "Settings", new List<string>() }
            };

            if (role == "User" && userControllerPermissions.ContainsKey(controllerName))
            {
                var allowedActions = userControllerPermissions[controllerName];
                return allowedActions.Contains(actionName) || 
                       allowedActions.Contains("*");
            }

            return false;
        }

        // Get accessible menu items for user
        public static List<MenuItem> GetMenuItems(ClaimsPrincipal user)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var menuItems = new List<MenuItem>();

            if (role == "Admin")
            {
                menuItems.AddRange(new List<MenuItem>
                {
                    new MenuItem { Name = "ផ្ទាំងគ្រប់គ្រង", Icon = "bi-grid", Controller = "Home", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងសិស្ស", Icon = "bi-people", Controller = "Student", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងថ្នាក់", Icon = "bi-journal-text", Controller = "StudentClass", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងវត្តមាន", Icon = "bi-calendar-check", Controller = "Attendance", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងពិន្ទុ", Icon = "bi-award", Controller = "Grade", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងបង់ប្រាក់", Icon = "bi-credit-card", Controller = "Payment", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងអ្នកប្រើប្រាស់", Icon = "bi-person-gear", Controller = "User", Action = "Index" },
                    new MenuItem { Name = "របាយការណ៍", Icon = "bi-file-earmark-bar-graph", Controller = "Report", Action = "Index" },
                    new MenuItem { Name = "កែសម្រួលប្រព័ន្ធ", Icon = "bi-gear", Controller = "Settings", Action = "Index" },
                    new MenuItem { Name = "កំណត់ហេតុសកម្មភាព", Icon = "bi-clock-history", Controller = "ActivityLog", Action = "Index" }
                });
            }
            else if (role == "User")
            {
                menuItems.AddRange(new List<MenuItem>
                {
                    new MenuItem { Name = "ផ្ទាំងដើម", Icon = "bi-house", Controller = "Home", Action = "Index" },
                    new MenuItem { Name = "មើលសិស្ស", Icon = "bi-people", Controller = "Student", Action = "Index" },
                    new MenuItem { Name = "មើលថ្នាក់", Icon = "bi-journal-text", Controller = "StudentClass", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងវត្តមាន", Icon = "bi-calendar-check", Controller = "Attendance", Action = "Index" },
                    new MenuItem { Name = "មើលពិន្ទុ", Icon = "bi-award", Controller = "Grade", Action = "Index" },
                    new MenuItem { Name = "គ្រប់គ្រងបង់ប្រាក់", Icon = "bi-credit-card", Controller = "Payment", Action = "Index" },
                    new MenuItem { Name = "របាយការណ៍", Icon = "bi-file-earmark-bar-graph", Controller = "Report", Action = "Index" }
                });
            }

            return menuItems;
        }

        // Check if user can modify data (Create, Edit, Delete)
        public static bool CanModify(ClaimsPrincipal user, string module)
        {
            return HasPermission(user, module, "Create") || 
                   HasPermission(user, module, "Edit") || 
                   HasPermission(user, module, "Delete");
        }

        // Check if user is Admin
        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value == "Admin";
        }

        // Check if user is regular User
        public static bool IsUser(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value == "User";
        }

        // Get current user ID
        public static int? GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }

    public class MenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
