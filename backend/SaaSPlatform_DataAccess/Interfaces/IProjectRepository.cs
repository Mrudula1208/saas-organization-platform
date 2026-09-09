using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IProjectRepository
    {
        // One paged query for a single tenant. Filtering and paging happen in the database.
        Task<PagedResult<Project>> GetProjectsPage(Guid tenantId, string? search, string? status, string? priority, int page, int pageSize);

        // Read-only list query that projects only fields used by the API and
        // calculates task totals in SQL instead of loading the task graph.
        Task<PagedResult<ProjectViewDto>> GetProjectViewsPage(Guid tenantId, string? search, string? status, string? priority, int page, int pageSize);

        // Tenant-scoped detail projection for read endpoints.
        Task<ProjectViewDto?> GetProjectViewByIdAsync(Guid id, Guid tenantId);

        // Lightweight tenant lookup for command validation; avoids loading the
        // project owner and task graph just to check ownership.
        Task<Guid?> GetTenantIdAsync(Guid id);

        // Get single project by ID
        Task<Project?> GetByIdAsync(Guid Id);

        // Check whether a project exists (used to distinguish 403 vs 404)
        Task<bool> ExistsAsync(Guid Id);

        // Add new project
        Task<Project> AddAsync(Project project);

        // Update project
        Task UpdateAsync(Project project);

        // Delete project
        Task DeleteAsync(Project project);

        // Count active projects for a tenant to enforce plan limits
        Task<int> CountActiveProjectsByTenantAsync(Guid tenantId);
    }
}
