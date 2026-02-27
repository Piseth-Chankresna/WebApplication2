using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- តារាងទិន្នន័យ ---
        public DbSet<Student> Students { get; set; }
        public DbSet<StudentClass> StudentClasses { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // កំណត់ទំហំលេខ decimal សម្រាប់ AmountDue (ឧទាហរណ៍៖ 18 ខ្ទង់ សល់ក្បៀស 2 ខ្ទង់)
            // បើមិនដាក់ទេ វានឹងមានបញ្ហាពេលរក្សាទុកចំនួនទឹកប្រាក់
            modelBuilder.Entity<Student>()
                .Property(s => s.AmountDue)
                .HasColumnType("decimal(18,2)");

            // ប្រសិនបើ StudentClass មាន Field ចំនួនទឹកប្រាក់ដែរ ក៏ត្រូវកំណត់ដូចគ្នា
            // modelBuilder.Entity<StudentClass>()
            //    .Property(c => c.Price)
            //    .HasColumnType("decimal(18,2)");
        }
    }
}