using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace SaaSPlatform.Tests
{
    /// <summary>
    /// Small helpers shared by the controller tests.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Puts a signed-in user (JWT claims) on the controller so we can test
        /// tenant/role checks without running the whole authentication pipeline.
        /// </summary>
        internal static void SetUser(ControllerBase controller, Guid? tenantId = null, string? role = null, Guid? userId = null)
        {
            var claims = new List<Claim>();
            if (tenantId.HasValue)
                claims.Add(new Claim("TenantId", tenantId.Value.ToString()));
            if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));
            if (userId.HasValue)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            };
        }

        /// <summary>
        /// Reads the "message" property from error responses that use anonymous objects
        /// like new { success = false, message = "..." }.
        /// </summary>
        internal static string? ReadMessage(object? value)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(value));
            if (json.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
            return null;
        }
    }
}
