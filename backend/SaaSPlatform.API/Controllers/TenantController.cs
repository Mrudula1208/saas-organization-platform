using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SaaSPlatform.API.Configurations;
using SaaSPlatform.Application.DTOS.Tenants;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private static readonly string[] AllowedLogoExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
        private const long MaxLogoSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly ITenantService _tenantService;
        private readonly IWebHostEnvironment _environment;
        private readonly StorageSettings _storageSettings;

        public TenantController(
            ITenantService tenantService,
            IWebHostEnvironment environment,
            IOptions<StorageSettings> storageSettings)
        {
            _tenantService = tenantService;
            _environment = environment;
            _storageSettings = storageSettings.Value;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAll()
        {
            var tenants = await _tenantService.GetAllAsync();
            return Ok(tenants);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Tenant>> GetById(Guid Id)
        {
            // Tenant isolation: a TenantAdmin may only load their own tenant.
            if (!User.IsInRole("SuperAdmin"))
            {
                var callerTenantId = GetTenantId();
                if (callerTenantId == null || callerTenantId.Value != Id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have access to this tenant." });
                }
            }

            var tenant = await _tenantService.GetByIdAsync(Id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant environment not found." });
            }
            return Ok(tenant);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantDto dto)
        {
            try
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Domain = dto.Domain,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    SubscriptionPlanId = dto.SubscriptionPlanId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _tenantService.CreateAsync(tenant);
                return CreatedAtAction(nameof(GetById), new { Id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{Id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult> Update(Guid Id, [FromBody] UpdateTenantDto dto)
        {
            try
            {
                var tenant = new Tenant
                {
                    Name = dto.Name,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    IsActive = dto.IsActive
                };

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
        [Authorize(Roles = "SuperAdmin")]
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
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        [RequestSizeLimit(MaxLogoSizeBytes)]
        public async Task<IActionResult> UploadLogo(Guid Id, [FromForm] IFormFile file)
        {
            try
            {
                var tenant = await _tenantService.GetByIdAsync(Id);
                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found." });
                }

                // Tenant isolation: a TenantAdmin may only change their own tenant's logo.
                if (!User.IsInRole("SuperAdmin"))
                {
                    var callerTenantId = GetTenantId();
                    if (callerTenantId == null || callerTenantId.Value != Id)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have access to this tenant." });
                    }
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file selected." });
                }

                if (file.Length > MaxLogoSizeBytes)
                {
                    return BadRequest(new { success = false, message = "Logo image must be 2 MB or smaller." });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedLogoExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Only PNG, JPG, JPEG, WEBP and GIF images are allowed." });
                }

                var logosPath = GetLogosPath();
                Directory.CreateDirectory(logosPath);

                // Generate a safe, unique filename. The client-supplied name/path is never trusted.
                var fileName = $"{Id:N}_{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(logosPath, fileName);
                var relativeUrl = $"/uploads/logos/{fileName}";

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Replace the previous logo file once the new one is safely saved.
                DeleteExistingLogo(tenant.LogoImageUrl);

                await _tenantService.UpdateLogoAsync(Id, relativeUrl);

                return Ok(new { success = true, logoUrl = relativeUrl, message = "Logo uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private string GetLogosPath()
        {
            var uploadsPath = Path.Combine(_environment.ContentRootPath, _storageSettings.UploadsPath);
            return Path.Combine(uploadsPath, "logos");
        }

        private void DeleteExistingLogo(string? existingLogoUrl)
        {
            if (string.IsNullOrWhiteSpace(existingLogoUrl))
            {
                return;
            }

            // Only ever delete file names, never paths, and only inside the logos folder.
            var fileName = Path.GetFileName(existingLogoUrl);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var logosPathFull = Path.GetFullPath(GetLogosPath());
            var fileFullPath = Path.GetFullPath(Path.Combine(logosPathFull, fileName));

            if (!fileFullPath.StartsWith(logosPathFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(fileFullPath))
            {
                System.IO.File.Delete(fileFullPath);
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