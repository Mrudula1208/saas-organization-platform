using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        // Inject service
        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null)
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
            if (tenantClaim == null) return Unauthorized();

            var tenantId = Guid.Parse(tenantClaim);
            var projects = await _projectService.GetAllAsync(tenantId, search, status, priority);
            return Ok(projects);
        }







        [HttpGet("{Id}")]
        public async Task<ActionResult<Project>> GetById(Guid Id)
        {
            var project = await _projectService.GetByIdAsync(Id);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }


        [HttpPost]
        public async Task<ActionResult<Project>> Create(CreateProjectDto dto)
        {
            var project = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
        {
            await _projectService.UpdateAsync(id, dto);

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _projectService.DeleteAsync(id);

            return NoContent();

        }
    }
}