using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using System.Security.Claims;

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
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var projects = await _projectService.GetAllAsync(tenantId.Value, search, status, priority);
            return Ok(projects);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Project>> GetById(Guid Id)
        {
            var project = await _projectService.GetByIdAsync(Id);
            if (project == null)
                return NotFound();

            return Ok(project);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult<Project>> Create(CreateProjectDto dto)
        {
            var project = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
        {
            await _projectService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _projectService.DeleteAsync(id);
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
}