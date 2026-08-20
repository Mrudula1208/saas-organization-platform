using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.SubscriptionPlans
{
    public class CreateSubscriptionPlanDto
    {
        [Required(ErrorMessage = "Plan name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxUsers { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxProjects { get; set; }

        [Range(1, int.MaxValue)]
        public int StorageLimitMB { get; set; }
    }
}
