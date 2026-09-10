using System;
using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Settings
{
    public class PlatformSettingsDto
    {
        public string PlatformName { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public bool MaintenanceMode { get; set; }
        public bool AllowRegistrations { get; set; }
        public bool MfaRequired { get; set; }
        public int SessionTimeout { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdatePlatformSettingsDto
    {
        [Required(ErrorMessage = "Platform name is required.")]
        [MaxLength(100, ErrorMessage = "Platform name must be 100 characters or fewer.")]
        public string PlatformName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Support email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid support email address.")]
        [MaxLength(150, ErrorMessage = "Support email must be 150 characters or fewer.")]
        public string SupportEmail { get; set; } = string.Empty;

        public bool MaintenanceMode { get; set; }
        public bool AllowRegistrations { get; set; }
        public bool MfaRequired { get; set; }

        [Range(5, 1440, ErrorMessage = "Session timeout must be between 5 and 1440 minutes.")]
        public int SessionTimeout { get; set; } = 30;
    }
}
