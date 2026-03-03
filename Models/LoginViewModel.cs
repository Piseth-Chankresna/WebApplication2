using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string? Email { get; set; }  // Make nullable with ?

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }  // Make nullable with ?

        public bool RememberMe { get; set; }
    }
}