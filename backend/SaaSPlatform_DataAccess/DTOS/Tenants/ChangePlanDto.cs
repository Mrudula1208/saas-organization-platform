using System;
using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Tenants
{
    public class ChangePlanDto
    {
        [Required(ErrorMessage = "Subscription plan ID is required.")]
        public Guid SubscriptionPlanId { get; set; }
    }
}
