using System;

namespace SaaSPlatform.Application.DTOS.Reports
{
    /// <summary>Tenant-specific project row used in PDF/Excel report exports.</summary>
    public class TenantProjectBreakdownDto
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
    }
}
