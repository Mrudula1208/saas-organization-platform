using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Auth
{
    public class TokenRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
