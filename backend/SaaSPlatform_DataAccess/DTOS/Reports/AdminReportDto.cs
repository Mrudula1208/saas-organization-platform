using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Reports
{
    public class AdminReportDto
    {
        public List<QuarterlyStatDto> QuarterlyTenants { get; set; } = new List<QuarterlyStatDto>();
        public List<MonthlyStatDto> MonthlyUsers { get; set; } = new List<MonthlyStatDto>();
        public int TotalTenants { get; set; }
        public int TotalUsers { get; set; }
        public double AvgLifetimeMonths { get; set; }
        public double CustomerAcquisitionCost { get; set; }
        public double ChurnRate { get; set; }
    }
}