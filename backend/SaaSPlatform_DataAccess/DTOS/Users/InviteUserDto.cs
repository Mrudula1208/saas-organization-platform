using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class InviteUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Member";
    }
}
