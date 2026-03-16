using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Grade
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public string? Subject { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Attendance { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Assignment { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal MidTerm { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal FinalExam { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Total { get; set; } = 0;

        public string? GradeLetter { get; set; }

        public string? Semester { get; set; }
        public string? AcademicYear { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [NotMapped]
        public string? StudentName { get; set; }
    }
}