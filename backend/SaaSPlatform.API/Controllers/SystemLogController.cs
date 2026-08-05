using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
            Guid? tenantId = null;
            
            // If user has a tenant association, restrict log lookup to their tenant context
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var parsedTenantId) && parsedTenantId != Guid.Empty)
            {
                tenantId = parsedTenantId;
            }

            var logs = await _systemLogRepository.GetAllAsync(tenantId, actionType, startDate, endDate);
            return Ok(logs);
        }
    }
}
