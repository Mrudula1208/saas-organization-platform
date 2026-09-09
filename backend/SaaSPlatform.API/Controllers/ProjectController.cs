using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProjectViewDto>>> GetProjects(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var projects = await _projectService.GetProjectsPage(tenantId.Value, search, status, priority, page, pageSize);
            return Ok(projects);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProjectViewDto>> GetById(Guid id)
        {
            var tenantId = GetTenantId();
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
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult<ProjectViewDto>> Create(CreateProjectDto dto)
        {
            var tenantId = GetTenantId();
            var userId = GetCurrentUserId();
            if (tenantId == null || userId == null) return Unauthorized();

            dto.TenantId = tenantId.Value;
            dto.OwnerId = userId.Value;

            try
            {
                var project = await _projectService.CreateAsync(dto);
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
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
        {
            var tenantId = GetTenantId();
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
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                await _projectService.DeleteAsync(id, tenantId.Value);
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
}