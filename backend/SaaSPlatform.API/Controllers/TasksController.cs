using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;

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
            var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
            if (tenantClaim == null) return Unauthorized();

            var tenantId = Guid.Parse(tenantClaim);
            var tasks = await _taskService.GetAllAsync(tenantId, projectId, status, search);
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
    
    }

    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}

//context.Request.Headers["TenantId"]
//👉 Read TenantId from request

//context.Items["TenantId"] = tenantId
//👉 Store TenantId globally

//HttpContext.Items["TenantId"]
//👉 Get TenantId in controller

//_taskService.GetAllAsync(tenantId)
//👉 Send tenantId to service

//_taskRepository.GetAllAsync(tenantId)
//👉 Service passes tenantId to repository

//_context.Tasks
//👉 Access Tasks table

//Include(t → Project)
//👉 Load related Project

//Where(t → Project.TenantId == tenantId)
//👉 🔥 Filter tasks of that tenant

//ToListAsync()
//👉 Execute query and get datacontext.Request.Headers["TenantId"]
//👉 Read TenantId from request

//context.Items["TenantId"] = tenantId
//👉 Store TenantId globally

//HttpContext.Items["TenantId"]
//👉 Get TenantId in controller

//_taskService.GetAllAsync(tenantId)
//👉 Send tenantId to service

//_taskRepository.GetAllAsync(tenantId)
//👉 Service passes tenantId to repository

//_context.Tasks
//👉 Access Tasks table

//Include(t → Project)
//👉 Load related Project

//Where(t → Project.TenantId == tenantId)
//👉 🔥 Filter tasks of that tenant

//ToListAsync()
//👉 Execute query and get data


//Task → Project → Tenant → Filter