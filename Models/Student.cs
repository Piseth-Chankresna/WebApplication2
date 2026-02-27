using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Student
    {
        [Key]
        public string Id { get; set; } // ប្តូរទៅជា string ដើម្បីស៊ីគ្នាជាមួយ Controller (ID: Date.now() ពី JS)

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះសិស្ស")]
        [StringLength(100)]
        [Display(Name = "ឈ្មោះសិស្ស")]
        public string Name { get; set; }

        [Required(ErrorMessage = "សូមជ្រើសរើសភេទ")]
        [Display(Name = "ភេទ")]
        public string Gender { get; set; }

        [Display(Name = "ថ្ងៃខែឆ្នាំកំណើត")]
        public string Dob { get; set; }

        [Display(Name = "ទីកន្លែងកំណើត")]
        public string Pob { get; set; }

        [Display(Name = "លេខទូរស័ព្ទ")]
        public string Phone { get; set; }

        [Display(Name = "កម្រិតសិក្សា")]
        public string Degree { get; set; }

        [Display(Name = "ជំនាញ")]
        public string Major { get; set; }

        [Display(Name = "ឆ្នាំទី")]
        public string Year { get; set; }

        [Display(Name = "អាហារូបករណ៍")]
        public string Scholarship { get; set; }

        [Display(Name = "បន្ទប់")]
        public string Room { get; set; }

        [Display(Name = "ជំនាន់")]
        public string Batch { get; set; }

        [Display(Name = "រូបថត")]
        public string Photo { get; set; } // រក្សាទុកជា Base64 string

        // --- Field ថ្មីៗដែលបងចង់បាន ---

        [Display(Name = "ឆ្នាំសិក្សា")]
        public string AcademicYear { get; set; }

        [Display(Name = "ចំនួនទឹកប្រាក់")]
        public string AmountDue { get; set; } // ប្រើ string ដើម្បីងាយស្រួលបង្ហាញ "0.00" ក្នុង JSON

        [Display(Name = "ស្ថានភាព")]
        public string Status { get; set; } = "Pending";

        // --- បន្ថែមថ្ងៃខែឆ្នាំបញ្ចូលទិន្នន័យ ---
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string FullName => Name;
    }
}