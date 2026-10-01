using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.Interfaces;
using System;
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
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // SuperAdmin sees global system logs (all tenants).
            // Everyone else only sees the logs of their own tenant.
            Guid? tenantId = null;
            if (!User.IsSuperAdmin())
            {
                tenantId = User.GetTenantId();
                if (tenantId == null) return Unauthorized();
            }

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var logs = await _systemLogRepository.GetLogsPage(tenantId, actionType, search, startDate, endDate, page, pageSize);
            return Ok(logs);
        }
    }
}
