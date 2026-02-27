using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    // ១. សម្រាប់រក្សាទុកព័ត៌មានអ្នកប្រើប្រាស់ (UserAccount)
    public class UserAccount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលឈ្មោះពេញ")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលអ៊ីមែល")]
        [EmailAddress(ErrorMessage = "ទម្រង់អ៊ីមែលមិនត្រឹមត្រូវ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "សូមជ្រើសរើសតួនាទី")]
        public string Role { get; set; }

        [Required(ErrorMessage = "សូមបញ្ចូលលេខសម្ងាត់")]
        public string Password { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ២. សម្រាប់រក្សាទុកសិទ្ធិតាម Module នីមួយៗ (RolePermission)
    public class RolePermission
    {
        public string ModuleName { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
    }
}