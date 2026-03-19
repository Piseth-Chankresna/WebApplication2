using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះសិស្ស")]
        [StringLength(100, ErrorMessage = "ឈ្មោះមិនអាចលើសពី 100 តួអក្សរ")]
        [Display(Name = "ឈ្មោះសិស្ស")]
        public string FullName { get; set; } = string.Empty;

        // Legacy property for backward compatibility
        [NotMapped]
        public string Name 
        { 
            get => FullName; 
            set => FullName = value; 
        }

        [Required(ErrorMessage = "សូមបញ្ចូលអ៊ីមែល")]
        [EmailAddress(ErrorMessage = "ទម្រង់អ៊ីមែលមិនត្រឹមត្រូវ")]
        [StringLength(100)]
        [Display(Name = "អ៊ីមែល")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "លេខទូរស័ព្ទ")]
        [Phone(ErrorMessage = "ទម្រង់លេខទូរស័ព្ទមិនត្រឹមត្រូវ")]
        [Column("Phone")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "សូមជ្រើសរើសភេទ")]
        [StringLength(20)]
        [Display(Name = "ភេទ")]
        [Column("Gender")]
        public string Gender { get; set; } = string.Empty;

        // Legacy property for backward compatibility
        [NotMapped]
        public string Dob 
        { 
            get => DateOfBirth ?? ""; 
            set => DateOfBirth = value; 
        }

        [StringLength(10)]
        [Display(Name = "ថ្ងៃកំណើត")]
        public string? DateOfBirth { get; set; }

        // Legacy property for backward compatibility
        [NotMapped]
        public string Pob 
        { 
            get => PlaceOfBirth ?? ""; 
            set => PlaceOfBirth = value; 
        }

        [StringLength(200)]
        [Display(Name = "ទីកំណើត")]
        [Column("PlaceOfBirth")]
        public string? PlaceOfBirth { get; set; }

        [StringLength(100)]
        [Display(Name = "កម្រិតសិក្សា")]
        [Column("Degree")]
        public string? Degree { get; set; }

        [StringLength(100)]
        [Display(Name = "ជំនាញ")]
        [Column("Major")]
        public string? Major { get; set; }

        [StringLength(20)]
        [Display(Name = "ឆ្នាំសិក្សា")]
        [Column("Year")]
        public string? Year { get; set; }

        public int? ClassId { get; set; }

        [StringLength(50)]
        [Display(Name = "វេន")]
        public string? Shift { get; set; }

        [StringLength(100)]
        [Display(Name = "បន្ទប់")]
        public string? Room { get; set; }

        [StringLength(50)]
        [Display(Name = "ឈុត")]
        [Column("Batch")]
        public string? Batch { get; set; }

        [StringLength(50)]
        [Display(Name = "ឆ្នាំសិក្សា")]
        [Column("AcademicYear")]
        public string? AcademicYear { get; set; }

        [StringLength(50)]
        [Display(Name = "អាហារូបករណ៍")]
        [Column("Scholarship")]
        public string? Scholarship { get; set; }

        [StringLength(20)]
        [Display(Name = "ស្ថានភាព")]
        public string Status { get; set; } = "កំពុងសិក្សា";

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ថ្លៃជើងគិត")]
        public decimal TuitionFee { get; set; } = 0;

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ប្រាក់បង់រួច")]
        public decimal PaidAmount { get; set; } = 0;

        // Legacy property for backward compatibility
        [NotMapped]
        public decimal AmountDue 
        { 
            get => TuitionFee - PaidAmount; 
            set { /* Calculated property - cannot be set directly */ } 
        }

        [StringLength(255)]
        [Display(Name = "រូបភាព")]
        public string? Photo { get; set; }

        [StringLength(500)]
        [Display(Name = "ចំណាំ")]
        public string? Notes { get; set; }

        [Display(Name = "សកម្មភាព")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "បង្កើតនៅ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "បង្កើតដោយ")]
        public int? CreatedBy { get; set; }

        [Display(Name = "កែប្រែនៅ")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "កែប្រែដោយ")]
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual StudentClass? Class { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Not mapped properties
        [NotMapped]
        [Display(Name = "ប្រាក់នៅសល់")]
        public decimal OutstandingBalance => TuitionFee - PaidAmount;

        [NotMapped]
        [Display(Name = "លេខសម្គាល់")]
        public string StudentId => "STU" + Id.ToString("D6");

        [NotMapped]
        [Display(Name = "ឈ្មោះពេញ")]
        public string? FullNameKhmer => string.IsNullOrEmpty(FullName) ? "មិនមានឈ្មោះ" : FullName;
    }
}
