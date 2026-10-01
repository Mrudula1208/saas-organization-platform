using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.DTOS.Settings;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Utility;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly IPlatformSettingsService _settingsService;

        public SettingsController(IPlatformSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return Ok(new ApiResponse<PlatformSettingsDto>
            {
                Success = true,
                Message = "Platform settings loaded successfully",
                Data = settings
            });
        }

        [HttpPut]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdatePlatformSettingsDto dto)
        {
            var adminUserId = User.GetUserId();
            var updated = await _settingsService.UpdateSettingsAsync(dto, adminUserId);
            return Ok(new ApiResponse<PlatformSettingsDto>
            {
                Success = true,
                Message = "System configuration updated successfully",
                Data = updated
            });
        }

        [HttpGet("public-config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicConfig()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return Ok(new
            {
                success = true,
                platformName = settings.PlatformName,
                supportEmail = settings.SupportEmail,
                maintenanceMode = settings.MaintenanceMode,
                allowRegistrations = settings.AllowRegistrations,
                mfaRequired = settings.MfaRequired
            });
        }
    }
}
