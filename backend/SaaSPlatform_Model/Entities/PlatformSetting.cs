using System;

namespace SaaSPlatform.Domain.Entities
{
    public class PlatformSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PlatformName { get; set; } = "SaaS Platform";
        public string SupportEmail { get; set; } = "support@saas.com";
        public bool MaintenanceMode { get; set; } = false;
        public bool AllowRegistrations { get; set; } = true;
        public bool MfaRequired { get; set; } = false;
        public int SessionTimeout { get; set; } = 30;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
