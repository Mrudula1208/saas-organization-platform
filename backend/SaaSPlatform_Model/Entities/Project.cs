using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform_Model
{
    public class Project
    {
        public Guid Id { get; set; }
        public String Name { get; set; }

        public string Description { get; set; }

        public Guid TenantId { get; set; }
        public Guid OwnerId { get; set; }

        public string Status { get; set; }

        public string Priority { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }= DateTime.UtcNow;


        public Tenant Tenant { get; set; }
        public User Owner { get; set; }
        public ICollection<TaskItem> Tasks { get; set; }



    }
}
