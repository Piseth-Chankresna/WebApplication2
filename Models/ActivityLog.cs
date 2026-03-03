using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string? UserName { get; set; }

        [Required]
        public string? Action { get; set; }

        public string? Description { get; set; }

        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? Status { get; set; } = "ជោគជ័យ";
    }
}