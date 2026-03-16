using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class StudentClass
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលលេខកូដថ្នាក់")]
        [StringLength(20, ErrorMessage = "លេខកូដថ្នាក់មិនអាចលើសពី 20 តួអក្សរ")]
        [Display(Name = "លេខកូដថ្នាក់")]
        public string? ClassCode { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះថ្នាក់")]
        [StringLength(100, ErrorMessage = "ឈ្មោះថ្នាក់មិនអាចលើសពី 100 តួអក្សរ")]
        [Display(Name = "ឈ្មោះថ្នាក់")]
        public string? ClassName { get; set; }

        [StringLength(100)]
        [Display(Name = "ជំនាញ")]
        public string? Major { get; set; }

        [StringLength(20)]
        [Display(Name = "ឆ្នាំ")]
        public string? Year { get; set; }

        [StringLength(20)]
        [Display(Name = "ឆមាស")]
        public string? Semester { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលឆ្នាំសិក្សា")]
        [StringLength(20)]
        [Display(Name = "ឆ្នាំសិក្សា")]
        public string? AcademicYear { get; set; }

        [Range(1, 100, ErrorMessage = "ចំនួនសិស្សអតិបរមាត្រូវចន្លោះរវាង 1 និង 100")]
        [Display(Name = "ចំនួនសិស្សអតិបរមា")]
        public int MaxStudents { get; set; } = 40;

        [Display(Name = "ស្ថានភាព")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "បង្កើតនៅ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "បង្កើតដោយ")]
        public int? CreatedBy { get; set; }

        // Legacy properties for compatibility with existing frontend
        [StringLength(50)]
        [Display(Name = "បន្ទប់")]
        public string? Room { get; set; }

        [StringLength(100)]
        [Display(Name = "ឈ្មោះគ្រូ")]
        public string? TeacherName { get; set; }

        [StringLength(255)]
        [Display(Name = "រូបគ្រូ")]
        public string? TeacherPhoto { get; set; }

        [StringLength(50)]
        [Display(Name = "ម៉ោង")]
        public string? Time { get; set; }

        [Display(Name = "ចំនួនសិស្ស")]
        public int StudentCount { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<Student>? Students { get; set; }

        [NotMapped]
        [Display(Name = "ចំនួនសិស្សបច្ចុប្បន្ន")]
        public int CurrentStudents 
        { 
            get => StudentCount; 
            set => StudentCount = value; 
        }

        [NotMapped]
        [Display(Name = "ភាគរយចុះឈ្មោះ")]
        public decimal EnrollmentPercentage => MaxStudents > 0 ? (decimal)StudentCount / MaxStudents * 100 : 0;

        [NotMapped]
        [Display(Name = "ចំនួនទីតាំងទំនេរ")]
        public int AvailableSlots => MaxStudents - StudentCount;
    }
}