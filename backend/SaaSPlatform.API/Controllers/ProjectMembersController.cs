using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IProjectMemberService _projectMemberService;

        public ProjectMembersController(IProjectMemberService projectMemberService)
        {
            _projectMemberService = projectMemberService;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<ProjectMemberDto>>> GetMembersByProject(Guid projectId)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var members = await _projectMemberService.GetMembersByProjectAsync(projectId, tenantId.Value);
            return Ok(members);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult<ProjectMemberDto>> AddMember([FromBody] AddProjectMemberDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            try
            {
                var member = await _projectMemberService.AddMemberAsync(dto, tenantId.Value);
                return CreatedAtAction(nameof(GetMembersByProject), new { projectId = member.ProjectId }, member);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{memberId}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> RemoveMember(Guid memberId)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var removed = await _projectMemberService.RemoveMemberAsync(memberId, tenantId.Value);
            if (!removed)
                return NotFound(new { success = false, message = "Project member not found." });

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