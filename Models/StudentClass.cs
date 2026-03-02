using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class StudentClass
    {
        [Key]
        public int Id { get; set; }
        public string? ClassCode { get; set; }
        public string? ClassName { get; set; }
        public string? Room { get; set; }
        public string? Time { get; set; }
        public int StudentCount { get; set; }
    }
}