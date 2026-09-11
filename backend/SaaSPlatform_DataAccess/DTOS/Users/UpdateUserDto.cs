using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Member";

        public string? ProfileImageUrl { get; set; }
    }
}
