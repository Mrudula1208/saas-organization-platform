using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform_Model.Entities
{
    public  class Tenant
    {
        public Guid Id { get; set; }

        public string Name { get; set; }    

        public string Domain { get; set; }

        public string ContactEmail { get; set; }
        public  string ContactPhone { get; set; }   

        public Guid SubscriptionPlanId { get; set; }    

        public bool IsActive { get; set; }

        public string? LogoImageUrl { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();
        // 👉 Initialize empty list so it's NOT required

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        // 👉 Same for projects

    }
}
