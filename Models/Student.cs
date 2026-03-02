using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Student
    {
        [Key]
        public string? Id { get; set; }
        public string? Name { get; set; }
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
        public decimal AmountDue { get; set; }
        public string? Status { get; set; }
    }
}