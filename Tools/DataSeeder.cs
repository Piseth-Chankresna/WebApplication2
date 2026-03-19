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

            // Check if classes already exist
            if (!await context.StudentClasses.AnyAsync())
            {
                // Create sample classes
                var sampleClasses = new List<StudentClass>
                {
                    new StudentClass
                    {
                        ClassCode = "IT-101",
                        ClassName = "កម្មវិធីកុំព្យូទ័រឆ្នាំទី១",
                        Major = "Information Technology",
                        Year = "1",
                        AcademicYear = "2025-2026",
                        TeacherName = "លោក សុខ សុភ័ក្រ",
                        Room = "A101",
                        Time = "៧:០០-៩:០០",
                        MaxStudents = 40,
                        StudentCount = 25,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = 1 // Admin user
                    },
                    new StudentClass
                    {
                        ClassCode = "IT-201",
                        ClassName = "កម្មវិធីកុំព្យូទ័រឆ្នាំទី២",
                        Major = "Information Technology",
                        Year = "2",
                        AcademicYear = "2025-2026",
                        TeacherName = "លោកស្រី ចាន់ សុធី",
                        Room = "A102",
                        Time = "៩:៣០-១១:៣០",
                        MaxStudents = 40,
                        StudentCount = 30,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = 1
                    },
                    new StudentClass
                    {
                        ClassCode = "BA-101",
                        ClassName = "គ្រប់គ្រងអាជីវកម្មឆ្នាំទី១",
                        Major = "Business Administration",
                        Year = "1",
                        AcademicYear = "2025-2026",
                        TeacherName = "លោក គង់ ចំរើន",
                        Room = "B201",
                        Time = "១:០០-៣:០០",
                        MaxStudents = 35,
                        StudentCount = 28,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = 1
                    },
                    new StudentClass
                    {
                        ClassCode = "ENG-301",
                        ClassName = "អក្សរសាស្រ្តអង់គ្លេសឆ្នាំទី៣",
                        Major = "English Literature",
                        Year = "3",
                        AcademicYear = "2025-2026",
                        TeacherName = "លោកស្រី លី ច័ន្ទរឫទ្ធិ",
                        Room = "C301",
                        Time = "៣:៣០-៥:៣០",
                        MaxStudents = 30,
                        StudentCount = 22,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = 1
                    },
                    new StudentClass
                    {
                        ClassCode = "LAW-201",
                        ClassName = "ច្បាប់ឆ្នាំទី២",
                        Major = "Law",
                        Year = "2",
                        AcademicYear = "2025-2026",
                        TeacherName = "លោក ហ៊ុន ស៊ុន",
                        Room = "D201",
                        Time = "៥:៣០-៨:៣០",
                        MaxStudents = 40,
                        StudentCount = 35,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = 1
                    }
                };

                context.StudentClasses.AddRange(sampleClasses);
                await context.SaveChangesAsync();
                Console.WriteLine("Sample classes created successfully!");
            }
            else
            {
                Console.WriteLine("Classes already exist in database.");
            }

            Console.WriteLine("Database seeded successfully!");
            Console.WriteLine("Admin Login: admin@university.edu / admin123");
            Console.WriteLine("User Login: user@university.edu / user123");
        }
    }
}
