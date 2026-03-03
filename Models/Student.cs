using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Student
    {
        [Key]
        public string? Id { get; set; }  // Make nullable

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះសិស្ស")]
        public string? Name { get; set; }  // Make nullable

        public string? Gender { get; set; }
        public string? Dob { get; set; }
        public string? Pob { get; set; }
        public string? Phone { get; set; }
        public string? Degree { get; set; }
        public string? Major { get; set; }
        public string? Year { get; set; }
        public string? Scholarship { get; set; }
        public string? Room { get; set; }
        public string? Batch { get; set; }
        public string? Photo { get; set; }
        public string? AcademicYear { get; set; }

        [DataType(DataType.Currency)]
        public decimal AmountDue { get; set; }

        public string? Status { get; set; }
        public string? Shift { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Payment>? Payments { get; set; }  // Make nullable
        public virtual ICollection<Attendance>? Attendances { get; set; }  // Make nullable
        public virtual ICollection<Grade>? Grades { get; set; }  // Make nullable
    }
}