using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllAsync(Guid tenantId, string? search = null, string? status = null, string? priority = null);
        Task<Project?> GetByIdAsync(Guid Id);
        Task<Project> CreateAsync(CreateProjectDto dto);
        Task UpdateAsync(Guid Id, UpdateProjectDto dto);
        Task DeleteAsync(Guid Id);
    }
}
