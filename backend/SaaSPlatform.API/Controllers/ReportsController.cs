using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;
using System.Security.Claims;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var data = await _reportService.GetTenantDashboardAsync(tenantId.Value);
            return Ok(data);
        }

        [HttpGet("admin-dashboard")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var data = await _reportService.GetSuperAdminDashboardAsync();
            return Ok(data);
        }

        [HttpGet("tenant-report")]
        public async Task<IActionResult> GetTenantReport()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var data = await _reportService.GetTenantReportAsync(tenantId.Value);
            return Ok(data);
        }

        [HttpGet("admin-report")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAdminReport()
        {
            var data = await _reportService.GetAdminReportAsync();
            return Ok(data);
        }

        // PDF/Excel exports: tenant id always comes from the JWT, never from the request.
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportPdf()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                var file = await _reportService.ExportTenantReportPdfAsync(tenantId.Value);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to generate PDF report: {ex.Message}" });
            }
        }

        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportExcel()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                var file = await _reportService.ExportTenantReportExcelAsync(tenantId.Value);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to generate Excel report: {ex.Message}" });
            }
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
