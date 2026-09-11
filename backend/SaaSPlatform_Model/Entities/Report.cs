using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }  // ✅ ADD THIS

        public string ReportType { get; set; } = string.Empty;

        public Guid GeneratedByUserId { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }= DateTime.UtcNow;
    }
}
