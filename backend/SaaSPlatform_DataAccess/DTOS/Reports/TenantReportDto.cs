using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Reports
{
    public class TenantReportDto
    {
        public List<MonthlyStatDto> MonthlyProjects { get; set; } = new List<MonthlyStatDto>();
        public List<MonthlyStatDto> MonthlyTasksCreated { get; set; } = new List<MonthlyStatDto>();
        public List<MonthlyStatDto> MonthlyTasksCompleted { get; set; } = new List<MonthlyStatDto>();
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TotalMembers { get; set; }
        public double AvgTasksPerMember { get; set; }
        public double CompletionRate { get; set; }
    }
}