using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Domain.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
   

        public decimal Price { get; set; }
      

        public int MaxUsers { get; set; }
       

        public int MaxProjects { get; set; }

        public int StorageLimitMB { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
