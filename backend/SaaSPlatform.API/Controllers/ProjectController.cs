using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ProjectViewDto>>> GetProjects(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var projects = await _projectService.GetAllAsync(tenantId.Value, search, status, priority);
            return Ok(projects);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProjectViewDto>> GetById(Guid id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var project = await _projectService.GetByIdAsync(id);
            if (project == null || project.TenantId != tenantId.Value)
                return NotFound(new { success = false, message = "Project not found." });

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
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existing = await _projectService.GetByIdAsync(id);
            if (existing == null || existing.TenantId != tenantId.Value)
                return NotFound(new { success = false, message = "Project not found." });

            try
            {
                await _projectService.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
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

            var existing = await _projectService.GetByIdAsync(id);
            if (existing == null || existing.TenantId != tenantId.Value)
                return NotFound(new { success = false, message = "Project not found." });

            try
            {
                await _projectService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
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