using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System.Security.Claims;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TaskItem>>> GetAllTasks(
            [FromQuery] Guid? projectId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var tasks = await _taskService.GetTasksPage(tenantId.Value, projectId, status, search, page, pageSize);
            return Ok(tasks);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existing = await _taskService.GetByIdAsync(id);
            if (existing == null || existing.TenantId != tenantId.Value)
            {
                return NotFound(new { success = false, message = "Task not found." });
            }

            var result = await _taskService.UpdateStatusAsync(id, dto.Status);
            if (!result)
            {
                return NotFound(new { success = false, message = "Task not found." });
            }
            return Ok(new { success = true, message = "Task status updated." });
        }

        [HttpGet("single/{Id}")]
        public async Task<ActionResult<TaskItem>> GetById(Guid Id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var task = await _taskService.GetByIdAsync(Id);
            if (task == null || task.TenantId != tenantId.Value)
                return NotFound();

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create(CreateTaskDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            dto.TenantId = tenantId.Value;
            if (string.IsNullOrWhiteSpace(dto.Name) && !string.IsNullOrWhiteSpace(dto.Title))
            {
                dto.Name = dto.Title;
            }
            if (string.IsNullOrWhiteSpace(dto.Title) && !string.IsNullOrWhiteSpace(dto.Name))
            {
                dto.Title = dto.Name;
            }
            if (dto.AssignedUserId == Guid.Empty)
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    dto.AssignedUserId = currentUserId.Value;
                }
            }

            var task = await _taskService.CreateAsync(dto);

            // Clear navigation references to avoid a JSON reference cycle
            // (the tracked Project will navigate back to this task).
            task.Project = null;
            task.AssignedUser = null;

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid Id, UpdateTaskDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existing = await _taskService.GetByIdAsync(Id);
            if (existing == null || existing.TenantId != tenantId.Value)
                return NotFound();

            await _taskService.UpdateAsync(Id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existing = await _taskService.GetByIdAsync(Id);
            if (existing == null || existing.TenantId != tenantId.Value)
                return NotFound();

            await _taskService.DeleteAsync(Id);
            return NoContent();
        }

        private Guid? GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                return tenantId;
            return null;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty)
                return userId;
            return null;
        }
    }

    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}