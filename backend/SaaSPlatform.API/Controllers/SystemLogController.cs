using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SystemLogController : ControllerBase
    {
        private readonly ISystemLogRepository _systemLogRepository;

        public SystemLogController(ISystemLogRepository systemLogRepository)
        {
            _systemLogRepository = systemLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // SuperAdmin sees global system logs (all tenants).
            // Everyone else only sees the logs of their own tenant.
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isSuperAdmin = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            Guid? tenantId = null;
            if (!isSuperAdmin)
            {
                tenantId = GetTenantId();
                if (tenantId == null) return Unauthorized();
            }

            var logs = await _systemLogRepository.GetAllAsync(tenantId, actionType, startDate, endDate);
            return Ok(logs);
        }

        private Guid? GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                return tenantId;
            return null;
        }
    }
}
