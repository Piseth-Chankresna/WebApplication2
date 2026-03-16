using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Data
{
    public static class DbInitializer
    {
        public static async Task SeedData(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if Admin user already exists
            var existingAdmin = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == "admin@hru.edu.kh");

            if (existingAdmin == null)
            {
                // Create default Admin user
                var adminUser = new UserAccount
                {
                    FullName = "System Administrator",
                    Email = "admin@hru.edu.kh",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.UserAccounts.Add(adminUser);
                await context.SaveChangesAsync();

                Console.WriteLine("✅ Default Admin user created successfully!");
                Console.WriteLine("📧 Email: admin@hru.edu.kh");
                Console.WriteLine("🔑 Password: Admin@123");
            }
            else
            {
                Console.WriteLine("ℹ️ Admin user already exists in database.");
            }

            // Seed default role permissions
            await SeedRolePermissions(context);
        }

        private static async Task SeedRolePermissions(ApplicationDbContext context)
        {
            // Check if permissions already exist
            var existingPermissions = await context.RolePermissions.AnyAsync();
            if (existingPermissions)
            {
                Console.WriteLine("ℹ️ Role permissions already exist.");
                return;
            }

            var permissions = new List<RolePermission>
            {
                // Admin permissions - Full access
                new() { RoleName = "Admin", ModuleName = "Students", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Users", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Payments", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Attendance", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Grades", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Reports", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },
                new() { RoleName = "Admin", ModuleName = "Settings", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true, CanExport = true },

                // User permissions - Limited access
                new() { RoleName = "User", ModuleName = "Students", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false, CanExport = false },
                new() { RoleName = "User", ModuleName = "Users", CanView = true, CanCreate = true, CanEdit = false, CanDelete = false, CanExport = false },
                new() { RoleName = "User", ModuleName = "Payments", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false, CanExport = true },
                new() { RoleName = "User", ModuleName = "Attendance", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false, CanExport = false },
                new() { RoleName = "User", ModuleName = "Grades", CanView = true, CanCreate = false, CanEdit = false, CanDelete = false, CanExport = false },
                new() { RoleName = "User", ModuleName = "Reports", CanView = true, CanCreate = false, CanEdit = false, CanDelete = false, CanExport = true },
                new() { RoleName = "User", ModuleName = "Settings", CanView = false, CanCreate = false, CanEdit = false, CanDelete = false, CanExport = false }
            };

            context.RolePermissions.AddRange(permissions);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Default role permissions created successfully!");
        }
    }
}
