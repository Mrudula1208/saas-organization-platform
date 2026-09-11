using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string FullName { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }
    }
}
