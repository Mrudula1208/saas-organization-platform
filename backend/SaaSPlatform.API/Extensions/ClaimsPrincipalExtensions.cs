using System;
using System.Security.Claims;

namespace SaaSPlatform.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetTenantId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst("TenantId")?.Value;
            if (claim != null && Guid.TryParse(claim, out var id) && id != Guid.Empty)
                return id;
            return null;
        }

        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claim != null && Guid.TryParse(claim, out var id) && id != Guid.Empty)
                return id;
            return null;
        }

        public static bool IsSuperAdmin(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("SuperAdmin");
        }

        public static string? GetRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.FindFirst("Role")?.Value;
        }
    }
}
