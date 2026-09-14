using System;

namespace SaaSPlatform.Application.DTOS.Users
{
    /// <summary>
    /// Safe profile representation of the authenticated user.
    /// Never exposes PasswordHash or any other sensitive credential data.
    /// </summary>
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
    }
}
