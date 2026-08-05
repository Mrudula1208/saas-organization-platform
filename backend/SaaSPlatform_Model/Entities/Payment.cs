using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        // 👉 Who made the payment

        public Guid TenantId { get; set; }
        // 👉 Multi-tenant support

        public Guid SubscriptionPlanId { get; set; }
        // 👉 Which plan purchased

        public decimal Amount { get; set; }
        // 👉 Money paid

        public string PaymentMethod { get; set; } = string.Empty;
        // 👉 UPI / Card

        public string PaymentStatus { get; set; } = string.Empty;
        // 👉 Success / Failed

        public string TransactionId { get; set; } = string.Empty;
        // 👉 Unique reference

        public DateTime PaymentDate { get; set; }
        // 👉 When payment happened

        // 🔗 Navigation (optional but good)
        public User User { get; set; }
    }
}
