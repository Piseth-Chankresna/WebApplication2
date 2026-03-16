using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<StudentClass> StudentClasses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Student configuration
            modelBuilder.Entity<Student>()
                .Property(s => s.TuitionFee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Student>()
                .Property(s => s.PaidAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(sc => sc.Students)
                .HasForeignKey(s => s.ClassId);

            // StudentClass configuration
            modelBuilder.Entity<StudentClass>()
                .HasIndex(sc => sc.ClassCode)
                .IsUnique();

            modelBuilder.Entity<StudentClass>()
                .HasOne<UserAccount>()
                .WithMany(ua => ua.CreatedClasses)
                .HasForeignKey(sc => sc.CreatedBy);

            // Payment configuration
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId);

            // Grade configuration
            modelBuilder.Entity<Grade>()
                .Property(g => g.Attendance)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Grade>()
                .Property(g => g.Assignment)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Grade>()
                .Property(g => g.MidTerm)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Grade>()
                .Property(g => g.FinalExam)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Grade>()
                .Property(g => g.Total)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId);

            // Attendance configuration
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Recorder)
                .WithMany()
                .HasForeignKey(a => a.RecordedBy);

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleName, rp.ModuleName })
                .IsUnique();

            // UserAccount configuration
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ActivityLog configuration
            modelBuilder.Entity<ActivityLog>()
                .Property(al => al.Timestamp)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ActivityLog>()
                .HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(al => al.UserId);
        }
    }
}
