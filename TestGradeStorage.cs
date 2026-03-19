using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Tools
{
    public class TestGradeStorage
    {
        public static async Task TestGradeOperations(ApplicationDbContext context)
        {
            Console.WriteLine("=== Testing Grade Storage ===");
            
            // Check if there are any students
            var students = await context.Students.ToListAsync();
            Console.WriteLine($"Found {students.Count} students in database");
            
            if (students.Count == 0)
            {
                Console.WriteLine("No students found. Creating test student...");
                
                var testStudent = new Student
                {
                    FullName = "Test Student",
                    Email = "teststudent@university.edu",
                    Gender = "Male",
                    Major = "Information Technology",
                    Year = "1",
                    Shift = "ព្រឹក",
                    Status = "កំពុងសិក្សា",
                    TuitionFee = 1000,
                    PaidAmount = 500,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                
                context.Students.Add(testStudent);
                await context.SaveChangesAsync();
                Console.WriteLine($"Created test student with ID: {testStudent.Id}");
                
                students.Add(testStudent);
            }
            
            // Check existing grades
            var existingGrades = await context.Grades.Include(g => g.Student).ToListAsync();
            Console.WriteLine($"Found {existingGrades.Count} existing grades");
            
            // Create a test grade
            var testGrade = new Grade
            {
                StudentId = students[0].Id,
                Subject = "Mathematics",
                Attendance = 8.5m,
                Assignment = 16.0m,
                MidTerm = 24.0m,
                FinalExam = 32.0m,
                Semester = "ឆមាសទី ១",
                AcademicYear = "2025-2026",
                RecordedAt = DateTime.Now
            };
            
            // Calculate total and grade letter
            testGrade.Total = testGrade.Attendance + testGrade.Assignment + testGrade.MidTerm + testGrade.FinalExam;
            
            if (testGrade.Total >= 90) testGrade.GradeLetter = "A";
            else if (testGrade.Total >= 80) testGrade.GradeLetter = "B";
            else if (testGrade.Total >= 70) testGrade.GradeLetter = "C";
            else if (testGrade.Total >= 60) testGrade.GradeLetter = "D";
            else if (testGrade.Total >= 50) testGrade.GradeLetter = "E";
            else testGrade.GradeLetter = "F";
            
            context.Grades.Add(testGrade);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Created test grade:");
            Console.WriteLine($"  Student: {students[0].FullName}");
            Console.WriteLine($"  Subject: {testGrade.Subject}");
            Console.WriteLine($"  Attendance: {testGrade.Attendance}");
            Console.WriteLine($"  Assignment: {testGrade.Assignment}");
            Console.WriteLine($"  MidTerm: {testGrade.MidTerm}");
            Console.WriteLine($"  FinalExam: {testGrade.FinalExam}");
            Console.WriteLine($"  Total: {testGrade.Total}");
            Console.WriteLine($"  Grade Letter: {testGrade.GradeLetter}");
            Console.WriteLine($"  Grade ID: {testGrade.Id}");
            
            // Verify the grade was saved
            var savedGrade = await context.Grades
                .Include(g => g.Student)
                .FirstOrDefaultAsync(g => g.Id == testGrade.Id);
                
            if (savedGrade != null)
            {
                Console.WriteLine("✅ Grade successfully stored and retrieved from database!");
                Console.WriteLine($"   Retrieved: {savedGrade.Student?.FullName} - {savedGrade.Subject} - {savedGrade.Total} ({savedGrade.GradeLetter})");
            }
            else
            {
                Console.WriteLine("❌ Failed to retrieve saved grade from database!");
            }
            
            Console.WriteLine("=== Grade Storage Test Complete ===");
        }
    }
}
