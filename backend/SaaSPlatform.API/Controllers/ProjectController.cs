using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly INotificationService? _notificationService;

        public ProjectController(IProjectService projectService, INotificationService? notificationService = null)
        {
            _projectService = projectService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProjectViewDto>>> GetProjects(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var projects = await _projectService.GetProjectsPage(tenantId.Value, search, status, priority, page, pageSize);
            return Ok(projects);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProjectViewDto>> GetById(Guid id)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var project = await _projectService.GetByIdAsync(id, tenantId.Value);
            if (project == null)
            {
                // The project exists but belongs to a different tenant.
                if (await _projectService.ExistsAsync(id))
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have access to this project." });

                return NotFound(new { success = false, message = "Project not found." });
            }

            return Ok(project);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
        public async Task<ActionResult<ProjectViewDto>> Create(CreateProjectDto dto)
        {
            var tenantId = User.GetTenantId();
            var userId = User.GetUserId();
            if (tenantId == null || userId == null) return Unauthorized();

            dto.TenantId = tenantId.Value;
            dto.OwnerId = userId.Value;

            try
            {
                var project = await _projectService.CreateAsync(dto);

                if (_notificationService != null)
                {
                    try
                    {
                        await _notificationService.CreateAsync(tenantId.Value, $"New project '{project.Name}' was created.");
                    }
                    catch { /* Best effort notification */ }
                }

                return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
        public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                await _projectService.UpdateAsync(id, tenantId.Value, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                await _projectService.DeleteAsync(id, tenantId.Value);

                if (_notificationService != null)
                {
                    try
                    {
                        await _notificationService.CreateAsync(tenantId.Value, "A project was deleted from the workspace.");
                    }
                    catch { /* Best effort notification */ }
                }

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = ex.Message });
            }
        }
    }
}