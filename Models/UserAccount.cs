using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះពេញ")]
        [Display(Name = "ឈ្មោះពេញ")]
        public string? FullName { get; set; }  // Make nullable

        [Required(ErrorMessage = "សូមបញ្ចូលអ៊ីមែល")]
        [EmailAddress(ErrorMessage = "ទម្រង់អ៊ីមែលមិនត្រឹមត្រូវ")]
        [Display(Name = "អ៊ីមែល")]
        public string? Email { get; set; }  // Make nullable

        [Required(ErrorMessage = "សូមជ្រើសរើសតួនាទី")]
        [Display(Name = "តួនាទី")]
        public string? Role { get; set; }  // Make nullable

        [Required(ErrorMessage = "សូមបញ្ចូលលេខសម្ងាត់")]
        [DataType(DataType.Password)]
        [Display(Name = "លេខសម្ងាត់")]
        public string? Password { get; set; }  // Make nullable

        [Display(Name = "សកម្ម")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "បង្កើតនៅ")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "លេខសម្ងាត់មិនត្រូវគ្នា")]
        [Display(Name = "បញ្ជាក់លេខសម្ងាត់")]
        public string? ConfirmPassword { get; set; }  // Make nullable
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }
        public string? RoleName { get; set; }  // Make nullable
        public string? ModuleName { get; set; }  // Make nullable
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
    }

    public class SystemDataWrapper
    {
        public List<UserAccount>? Users { get; set; }  // Make nullable
        public Dictionary<string, List<RolePermission>>? RolePermissions { get; set; }  // Make nullable
    }
}