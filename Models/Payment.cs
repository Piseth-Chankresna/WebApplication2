using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StudentName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ReceiptNumber { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [StringLength(20)]
        public string? PaymentMethod { get; set; }

        [StringLength(20)]
        public string? Semester { get; set; }

        [StringLength(20)]
        public string? AcademicYear { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(255)]
        public string? Note { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}