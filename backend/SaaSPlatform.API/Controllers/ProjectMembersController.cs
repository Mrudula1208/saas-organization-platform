using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System.Reflection.Metadata.Ecma335;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IProjectMemberService _projectservice;
        public ProjectMembersController(IProjectMemberService projectservice)
        {
            _projectservice = projectservice;
        }


        [HttpGet("{Id}")]
        public async Task<ActionResult>GetMembers()
        {
            var tenantId = (Guid)HttpContext.Items["TenantId"];
            var member = await _projectservice.GetMemberAsync(tenantId);
            return Ok(member);

        }


        [HttpPost]
        public async Task<ActionResult>AddMember(AddProjectMemberDto dto)
        {
            var member = await _projectservice.AddMemberAsync(dto);
            return Ok (member);
        }


        [HttpDelete("{Id}")]
        public async Task<IActionResult>RemoveAsync(Guid Id)
        {
            await _projectservice.RemoveAsync(Id);
            return NoContent();
        }
    }
}
