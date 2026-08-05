using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAll()
        {
            var tenants = await _tenantService.GetAllAsync();
            return Ok(tenants);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Tenant>> GetById(Guid Id)
        {
            var tenant = await _tenantService.GetByIdAsync(Id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant environment not found." });
            }
            return Ok(tenant);
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> Create([FromBody] Tenant tenant)
        {
            try
            {
                var created = await _tenantService.CreateAsync(tenant);
                return CreatedAtAction(nameof(GetById), new { Id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{Id}")]
        public async Task<ActionResult> Update(Guid Id, [FromBody] Tenant tenant)
        {
            try
            {
                var result = await _tenantService.UpdateAsync(Id, tenant);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Tenant not found." });
                }
                return Ok(new { success = true, message = "Tenant updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _tenantService.DeleteAsync(Id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }
            return Ok(new { success = true, message = "Tenant soft deleted successfully." });
        }

        [HttpPost("{Id}/upload-logo")]
        public async Task<IActionResult> UploadLogo(Guid Id, [FromForm] IFormFile file)
        {
            try
            {
                var tenant = await _tenantService.GetByIdAsync(Id);
                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found." });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file selected." });
                }

                // Simulate saving logo image to disk or using local URL path
                var fileName = $"{Id}_logo{Path.GetExtension(file.FileName)}";
                
                // Save logo URL in tenant settings
                var logoUrl = $"/assets/logos/{fileName}";
                await _tenantService.UpdateLogoAsync(Id, logoUrl);

                return Ok(new { success = true, logoUrl = logoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
