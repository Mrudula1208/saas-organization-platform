using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Domain.Entities
{
    public class TaskItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }
        public Guid AssignedUserId { get; set; }

        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid TenantId { get; set; }
        public Project Project { get; set; }
        public User AssignedUser { get; set; }

    }
}