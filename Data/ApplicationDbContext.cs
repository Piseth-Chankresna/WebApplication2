using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<StudentClass> StudentClasses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Maps the decimal to SQL correctly
            modelBuilder.Entity<Student>().Property(s => s.AmountDue).HasColumnType("decimal(18,2)");
        }
    }
}