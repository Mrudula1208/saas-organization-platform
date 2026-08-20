using System;
using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Payments
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid SubscriptionPlanId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
