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
        public string? StudentId { get; set; }  // Make nullable

        [Required]
        public string? StudentName { get; set; }  // Make nullable

        public string? Major { get; set; }
        public string? Year { get; set; }
        public string? Room { get; set; }
        public string? Semester { get; set; }
        public string? PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? ReceiptNumber { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }  // Make nullable
    }
}