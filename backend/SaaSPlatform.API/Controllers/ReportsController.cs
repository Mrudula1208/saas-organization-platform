using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
            if (tenantClaim == null) return Unauthorized();

            var tenantId = Guid.Parse(tenantClaim);
            var data = await _reportService.GetTenantDashboardAsync(tenantId);
            return Ok(data);    
        }

        [HttpGet("admin-dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            // Optional: verify user is in Admin role
            var data = await _reportService.GetSuperAdminDashboardAsync();
            return Ok(data);
        }
    }
}
