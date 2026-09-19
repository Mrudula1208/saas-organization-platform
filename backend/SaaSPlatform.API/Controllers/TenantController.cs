using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SaaSPlatform.API.Configurations;
using SaaSPlatform.Application.DTOS;
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
        private readonly ISubscriptionPlanRepository? _planRepository;

        public TenantController(
            ITenantService tenantService,
            IWebHostEnvironment environment,
            IOptions<StorageSettings> storageSettings,
            ISubscriptionPlanRepository? planRepository = null)
        {
            _tenantService = tenantService;
            _environment = environment;
            _storageSettings = storageSettings.Value;
            _planRepository = planRepository;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<PagedResult<Tenant>>> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] string? plan = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var tenants = await _tenantService.GetTenantsPage(search, plan, page, pageSize);
            return Ok(tenants);
        }

        // Tenant settings: the tenant id always comes from the JWT, never from the request.
        [HttpGet("settings")]
        public async Task<ActionResult> GetSettings()
        {
            var tenantId = GetTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { success = false, message = "No tenant found for the authenticated user." });
            }

            var settings = await _tenantService.GetSettingsAsync(tenantId.Value);
            if (settings == null)
            {
                return NotFound(new { success = false, message = "Tenant settings not found." });
            }

            return Ok(new { success = true, message = "Tenant settings loaded.", data = settings });
        }

        [HttpPut("settings")]
        [Authorize(Roles = "TenantAdmin")]
        public async Task<ActionResult> UpdateSettings([FromBody] TenantSettingsDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null)
            {
                return Unauthorized(new { success = false, message = "No tenant found for the authenticated user." });
            }

            try
            {
                var result = await _tenantService.UpdateSettingsAsync(tenantId.Value, dto, GetUserId());
                if (!result)
                {
                    return NotFound(new { success = false, message = "Tenant settings not found." });
                }

                return Ok(new { success = true, message = "Tenant settings saved successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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

                var created = await _tenantService.CreateAsync(tenant, GetUserId());
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
                // Tenant isolation: a TenantAdmin may only update their own tenant.
                if (!User.IsInRole("SuperAdmin"))
                {
                    var callerTenantId = GetTenantId();
                    if (callerTenantId == null || callerTenantId.Value != Id)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have access to this tenant." });
                    }
                }

                // TenantAdmins cannot change the active status; only SuperAdmins can.
                bool applyStatus = dto.IsActive;
                if (!User.IsInRole("SuperAdmin"))
                {
                    var existing = await _tenantService.GetByIdAsync(Id);
                    if (existing == null)
                    {
                        return NotFound(new { success = false, message = "Tenant not found." });
                    }
                    applyStatus = existing.IsActive;
                }

                var tenant = new Tenant
                {
                    Name = dto.Name,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    IsActive = applyStatus
                };

                var result = await _tenantService.UpdateAsync(Id, tenant, GetUserId());
                if (!result)
                {
                    return NotFound(new { success = false, message = "Tenant not found." });
                }

                // SuperAdmins can change the subscription plan
                if (User.IsInRole("SuperAdmin") && dto.SubscriptionPlanId.HasValue && dto.SubscriptionPlanId.Value != Guid.Empty)
                {
                    await _tenantService.ChangePlanAsync(Id, dto.SubscriptionPlanId.Value, GetUserId());
                }

                return Ok(new { success = true, message = "Tenant updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{Id}/plan")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ChangePlan(Guid Id, [FromBody] ChangePlanDto dto)
        {
            try
            {
                var result = await _tenantService.ChangePlanAsync(Id, dto.SubscriptionPlanId, GetUserId());
                if (!result)
                {
                    return NotFound(new { success = false, message = "Tenant not found." });
                }
                return Ok(new { success = true, message = "Subscription plan updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _tenantService.DeleteAsync(Id, GetUserId());
            if (!result)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }
            return Ok(new { success = true, message = "Tenant soft deleted successfully." });
        }

        [HttpPost("{Id}/upload-logo")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        [RequestSizeLimit(MaxLogoSizeBytes)]
        public async Task<IActionResult> UploadLogo(Guid Id, IFormFile file)
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

                // Enforce subscription storage limit
                if (_planRepository != null && tenant.SubscriptionPlanId != Guid.Empty)
                {
                    var plan = await _planRepository.GetByIdAsync(tenant.SubscriptionPlanId);
                    if (plan != null && plan.StorageLimitMB > 0)
                    {
                        long currentUsageBytes = 0;
                        if (Directory.Exists(logosPath))
                        {
                            var dirInfo = new DirectoryInfo(logosPath);
                            currentUsageBytes = dirInfo.GetFiles($"{Id:N}_*").Sum(f => f.Length);
                        }

                        long oldLogoBytes = 0;
                        if (!string.IsNullOrWhiteSpace(tenant.LogoImageUrl))
                        {
                            var oldFileName = Path.GetFileName(tenant.LogoImageUrl);
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                var oldFilePath = Path.Combine(logosPath, oldFileName);
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    oldLogoBytes = new FileInfo(oldFilePath).Length;
                                }
                            }
                        }

                        long newProjectedBytes = Math.Max(0, currentUsageBytes - oldLogoBytes) + file.Length;
                        long maxLimitBytes = (long)plan.StorageLimitMB * 1024 * 1024;
                        if (newProjectedBytes > maxLimitBytes)
                        {
                            return BadRequest(new { 
                                success = false, 
                                message = $"Storage limit of {plan.StorageLimitMB} MB reached for the {plan.Name} plan. Please upgrade your subscription to upload more files." 
                            });
                        }
                    }
                }

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

                await _tenantService.UpdateLogoAsync(Id, relativeUrl, GetUserId());

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

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (claim != null && Guid.TryParse(claim, out var userId) && userId != Guid.Empty)
                return userId;
            return null;
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