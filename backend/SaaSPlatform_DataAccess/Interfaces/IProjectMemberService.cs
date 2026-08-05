using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectMemberService
    {
        Task<IEnumerable<ProjectMember>> GetMemberAsync(Guid tenantId);
        Task<ProjectMember> AddMemberAsync(AddProjectMemberDto dto);
        Task RemoveAsync(Guid id);
    }
}
