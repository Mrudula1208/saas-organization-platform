using System;

namespace SaaSPlatform.Application.DTOS.Auth
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
