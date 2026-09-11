using System;

namespace SaaSPlatform.Application.DTOS.Notifications
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
