using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Tools
{
    public class DataSeeder
    {
        public static async Task SeedData(ApplicationDbContext context)
        {
            // Check if users already exist
            if (await context.UserAccounts.AnyAsync())
            {
                Console.WriteLine("Users already exist in database.");
                return;
            }

            // Create Admin user
            var adminUser = new UserAccount
            {
                FullName = "System Administrator",
                Email = "admin@university.edu",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // Create regular User
            var regularUser = new UserAccount
            {
                FullName = "Test User",
                Email = "user@university.edu", 
                Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            context.UserAccounts.AddRange(adminUser, regularUser);
            await context.SaveChangesAsync();

            // Create role permissions
            var permissions = new List<RolePermission>
            {
                // Admin permissions - full access
                new RolePermission
                {
                    RoleName = "Admin",
                    ModuleName = "Students",
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "Admin",
                    ModuleName = "StudentClass",
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "Admin",
                    ModuleName = "Grades",
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "Admin",
                    ModuleName = "Users",
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    CanExport = true
                },
                // User permissions - limited access
                new RolePermission
                {
                    RoleName = "User",
                    ModuleName = "Students",
                    CanView = true,
                    CanCreate = false,
                    CanEdit = false,
                    CanDelete = false,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "User",
                    ModuleName = "StudentClass",
                    CanView = true,
                    CanCreate = false,
                    CanEdit = false,
                    CanDelete = false,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "User",
                    ModuleName = "Grades",
                    CanView = true,
                    CanCreate = false,
                    CanEdit = false,
                    CanDelete = false,
                    CanExport = true
                },
                new RolePermission
                {
                    RoleName = "User",
                    ModuleName = "Users",
                    CanView = false,
                    CanCreate = false,
                    CanEdit = false,
                    CanDelete = false,
                    CanExport = false
                }
            };

            context.RolePermissions.AddRange(permissions);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded successfully!");
            Console.WriteLine("Admin Login: admin@university.edu / admin123");
            Console.WriteLine("User Login: user@university.edu / user123");
        }
    }
}
