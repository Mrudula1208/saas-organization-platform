using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectMemberRepository
    {
        // Get project members for a specific project, scoped to the tenant owning the project
        Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(Guid projectId, Guid tenantId);
        Task<ProjectMember?> GetByIdAsync(Guid id);
        Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId);
        Task<ProjectMember> AddAsync(ProjectMember member);
        Task DeleteAsync(ProjectMember member);
    }
}