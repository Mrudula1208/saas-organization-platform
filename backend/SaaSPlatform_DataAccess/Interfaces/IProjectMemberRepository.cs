using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectMemberRepository
    {
        //Get project members by tenant
       Task<IEnumerable<ProjectMember>> GetMembersAsync(Guid tenantId);
        // Add a new project member
        Task<ProjectMember> AddAsync(ProjectMember member);
        Task<ProjectMember> GetByIdAsync(Guid Id);
        Task  DeleteAsync(ProjectMember member);
    }
}
