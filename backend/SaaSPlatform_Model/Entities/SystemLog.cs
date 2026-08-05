using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Domain.Entities
{
    public class SystemLog
    {
        public Guid Id { get; set; }

        public string Action { get; set; }

        public string Description { get; set; }

        public Guid? UserId { get; set; }

        public Guid? TenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
