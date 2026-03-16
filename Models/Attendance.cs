using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        public string? AttendanceCode { get; set; }

        [Required]
        public int StudentId { get; set; }  // Changed from string to int to match Student.Id

        [Required]
        public string? StudentName { get; set; }  // Make nullable

        public string? Room { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(1)]
        [RegularExpression("[PLA]", ErrorMessage = "ស្ថានភាពត្រូវតែជា P, L, ឬ A")]
        public string? Status { get; set; }  // Make nullable

        public string? Note { get; set; }

        public int? RecordedBy { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }  // Make nullable

        [ForeignKey("RecordedBy")]
        public virtual UserAccount? Recorder { get; set; }  // Make nullable
    }
}