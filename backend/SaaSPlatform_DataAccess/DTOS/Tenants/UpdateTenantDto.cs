using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Tenants
{
    public class UpdateTenantDto
    {
        [Required(ErrorMessage = "Tenant name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
