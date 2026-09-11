using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
