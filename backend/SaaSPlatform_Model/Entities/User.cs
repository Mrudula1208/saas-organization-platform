using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform_Model
{
    public class User
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public Guid TenantId { get; set; }

        public bool IsActive { get; set; }

        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLogin { get; set; }

        // 🔐 Security & Refresh Tokens
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // 📧 Email Verification
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        // 🔑 Password Reset
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiryTime { get; set; }

        // 🚫 Account Lockout
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        // 🗑️ Soft Delete
        public bool IsDeleted { get; set; } = false;

        public Tenant? Tenant { get; set; }

        public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    }
}