using SaaSPlatform.Application.DTOS.Projects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectViewDto>> GetAllAsync(Guid tenantId, string? search = null, string? status = null, string? priority = null);
        Task<ProjectViewDto?> GetByIdAsync(Guid Id, Guid tenantId);
        Task<bool> ExistsAsync(Guid Id);
        Task<ProjectViewDto> CreateAsync(CreateProjectDto dto);
        Task UpdateAsync(Guid Id, Guid tenantId, UpdateProjectDto dto);
        Task DeleteAsync(Guid Id, Guid tenantId);
    }
}