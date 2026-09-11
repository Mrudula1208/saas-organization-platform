using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Security.Claims;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IProjectMemberService _projectservice;
        public ProjectMembersController(IProjectMemberService projectservice)
        {
            _projectservice = projectservice;
        }

        [HttpGet]
        public async Task<ActionResult> GetMembers()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var member = await _projectservice.GetMemberAsync(tenantId.Value);
            return Ok(member);
        }

        [HttpPost]
        public async Task<ActionResult> AddMember([FromBody] AddProjectMemberDto dto)
        {
            var member = await _projectservice.AddMemberAsync(dto);
            return Ok(member);
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> RemoveAsync(Guid Id)
        {
            await _projectservice.RemoveAsync(Id);
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
