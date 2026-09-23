using System;

namespace SaaSPlatform.Application.DTOS.Reports
{
    /// <summary>Platform-wide tenant row used in SuperAdmin PDF/Excel report exports.</summary>
    public class AdminTenantBreakdownDto
    {
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int ProjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
