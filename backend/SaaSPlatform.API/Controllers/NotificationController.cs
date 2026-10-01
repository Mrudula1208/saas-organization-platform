using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.DTOS.Notifications;
using SaaSPlatform_Utility;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var notifications = await _notificationService.GetAllAsync(tenantId.Value);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = notifications
            });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(tenantId.Value);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = count
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Message is required." });

            var notification = await _notificationService.CreateAsync(tenantId.Value, dto.Message);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = notification
            });
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var result = await _notificationService.MarkReadAsync(id, tenantId.Value);
            if (!result) return NotFound();

            return Ok(new ApiResponse<object> { Success = true });
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            await _notificationService.MarkAllReadAsync(tenantId.Value);
            return Ok(new ApiResponse<object> { Success = true });
        }

        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAll()
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            await _notificationService.ClearAllAsync(tenantId.Value);
            return Ok(new ApiResponse<object> { Success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var result = await _notificationService.DeleteAsync(id, tenantId.Value);
            if (!result) return NotFound();

            return Ok(new ApiResponse<object> { Success = true });
        }
    }
}
