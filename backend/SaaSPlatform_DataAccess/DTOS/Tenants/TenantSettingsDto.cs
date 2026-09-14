using System;
using System.ComponentModel.DataAnnotations;

namespace SaaSPlatform.Application.DTOS.Tenants
{
    /// <summary>
    /// Tenant settings returned by GET api/Tenant/settings and accepted by PUT api/Tenant/settings.
    /// The tenant id is always derived from the JWT on the server; it is never read from the request.
    /// </summary>
    public class TenantSettingsDto
    {
        public Guid Id { get; set; }

        public string Domain { get; set; } = string.Empty;

        public string? LogoImageUrl { get; set; }

        [Required(ErrorMessage = "Workspace name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Contact email is not a valid email address.")]
        public string ContactEmail { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public bool EmailNotifications { get; set; } = true;

        public bool InAppNotifications { get; set; } = true;
    }
}
