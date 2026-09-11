using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Auth
{
    public class RegisterTenantDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public string AdminName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Plan { get; set; } = "Basic";
    }
}
