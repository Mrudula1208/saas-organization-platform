using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;

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
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetAllTasks(
            [FromQuery] Guid? projectId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var tasks = await _taskService.GetAllAsync(tenantId.Value, projectId, status, search);
            return Ok(tasks);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
        {
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
            var task = await _taskService.GetByIdAsync(Id);
            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create(CreateTaskDto dto)
        {
            var task = await _taskService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid Id, UpdateTaskDto dto)
        {
            await _taskService.UpdateAsync(Id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
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
    }

    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}