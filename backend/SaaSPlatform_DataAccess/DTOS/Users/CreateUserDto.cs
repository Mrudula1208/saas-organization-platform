using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Name is Required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
