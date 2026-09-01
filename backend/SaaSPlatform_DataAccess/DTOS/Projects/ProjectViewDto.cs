using System;

namespace SaaSPlatform.Application.DTOS.Projects
{
    public class ProjectViewDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid TenantId { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
        public string Priority { get; set; } = "Medium";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public int Progress { get; set; }
    }
}