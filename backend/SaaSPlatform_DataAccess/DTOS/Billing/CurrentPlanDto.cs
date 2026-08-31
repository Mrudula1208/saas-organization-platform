using System;

namespace SaaSPlatform.Application.DTOS.Billing
{
    public class CurrentPlanDto
    {
        public Guid SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxUsers { get; set; }
        public int MaxProjects { get; set; }
        public int StorageLimitMB { get; set; }
        public string BillingFrequency { get; set; } = "Monthly";
        public DateTime? NextBillingDate { get; set; }
    }
}