using SaaSPlatform.Application.DTOS.ProjectMembers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectMemberService
    {
        Task<IEnumerable<ProjectMemberDto>> GetMembersByProjectAsync(Guid projectId, Guid tenantId);
        Task<ProjectMemberDto> AddMemberAsync(AddProjectMemberDto dto, Guid tenantId);
        Task<bool> RemoveMemberAsync(Guid memberId, Guid tenantId);
    }
}