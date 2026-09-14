using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectService
    {
        // One page of the tenant project list. Filtering and paging happen in the database.
        Task<PagedResult<ProjectViewDto>> GetProjectsPage(Guid tenantId, string? search = null, string? status = null, string? priority = null, int page = 1, int pageSize = 20);
        Task<ProjectViewDto?> GetByIdAsync(Guid Id, Guid tenantId);
        Task<bool> ExistsAsync(Guid Id);
        Task<ProjectViewDto> CreateAsync(CreateProjectDto dto);
        Task UpdateAsync(Guid Id, Guid tenantId, UpdateProjectDto dto);
        Task DeleteAsync(Guid Id, Guid tenantId);
    }
}