using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class StudentClass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? ClassCode { get; set; }  // Make nullable

        [Required]
        public string? ClassName { get; set; }  // Make nullable

        public string? Room { get; set; }
        public string? Time { get; set; }
        public int StudentCount { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPhoto { get; set; }
    }
}