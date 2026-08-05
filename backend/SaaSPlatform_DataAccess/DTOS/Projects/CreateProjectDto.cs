using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Projects
{
    public class CreateProjectDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid TenantId { get; set; }

        public Guid OwnerId { get; set; }

        public string Status { get; set; } = "Active";

        public string Priority { get; set; } = "Medium";

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    
}
}
