using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
    {
        public class ProjectRepository : IProjectRepository
        {
            private readonly ApplicationDbContext _context;

            public ProjectRepository(ApplicationDbContext context)
            {
                _context = context;
            }

            // Paged, filtered project list for one tenant. Filtering and paging happen in the database.
            public async Task<PagedResult<Project>> GetProjectsPage(Guid tenantId, string? search, string? status, string? priority, int page, int pageSize)
            {
                var query = _context.Projects.AsNoTracking()
                    .Where(p => p.TenantId == tenantId && !p.IsDeleted);

                if (!string.IsNullOrEmpty(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(lowerSearch) ||
                        (p.Description != null && p.Description.ToLower().Contains(lowerSearch)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var lowerStatus = status.ToLower();
                    query = query.Where(p => (p.Status ?? string.Empty).ToLower() == lowerStatus);
                }

                if (!string.IsNullOrEmpty(priority))
                {
                    var lowerPriority = priority.ToLower();
                    query = query.Where(p => (p.Priority ?? string.Empty).ToLower() == lowerPriority);
                }

                var totalCount = await query.CountAsync();

                var projects = await query
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    // Newest first; Id breaks ties so pages never skip or repeat a row.
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<Project>
                {
                    Data = projects,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }

            public async Task<PagedResult<ProjectViewDto>> GetProjectViewsPage(Guid tenantId, string? search, string? status, string? priority, int page, int pageSize)
            {
                var query = _context.Projects.AsNoTracking()
                    .Where(p => p.TenantId == tenantId && !p.IsDeleted);

                if (!string.IsNullOrEmpty(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(lowerSearch) ||
                        (p.Description != null && p.Description.ToLower().Contains(lowerSearch)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var lowerStatus = status.ToLower();
                    query = query.Where(p => (p.Status ?? string.Empty).ToLower() == lowerStatus);
                }

                if (!string.IsNullOrEmpty(priority))
                {
                    var lowerPriority = priority.ToLower();
                    query = query.Where(p => (p.Priority ?? string.Empty).ToLower() == lowerPriority);
                }

                var totalCount = await query.CountAsync();
                var projects = await query
                    .Select(p => new ProjectViewDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? string.Empty,
                        TenantId = p.TenantId,
                        OwnerId = p.OwnerId,
                        OwnerName = p.Owner.FullName,
                        Status = p.Status ?? "Pending",
                        Priority = p.Priority ?? "Medium",
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        TaskCount = p.Tasks.Count(t => !t.IsDeleted),
                        CompletedTaskCount = p.Tasks.Count(t => !t.IsDeleted && (t.IsCompleted || t.Status == "Completed"))
                    })
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                foreach (var project in projects)
                {
                    project.Progress = project.TaskCount > 0
                        ? (int)Math.Round((double)project.CompletedTaskCount / project.TaskCount * 100)
                        : 0;
                }

                return new PagedResult<ProjectViewDto>
                {
                    Data = projects,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }

            public async Task<ProjectViewDto?> GetProjectViewByIdAsync(Guid id, Guid tenantId)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted)
                    .Select(p => new ProjectViewDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? string.Empty,
                        TenantId = p.TenantId,
                        OwnerId = p.OwnerId,
                        OwnerName = p.Owner.FullName,
                        Status = p.Status ?? "Pending",
                        Priority = p.Priority ?? "Medium",
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        TaskCount = p.Tasks.Count(t => !t.IsDeleted),
                        CompletedTaskCount = p.Tasks.Count(t => !t.IsDeleted && (t.IsCompleted || t.Status == "Completed"))
                    })
                    .FirstOrDefaultAsync();

                if (project == null)
                {
                    return null;
                }

                project.Progress = project.TaskCount > 0
                    ? (int)Math.Round((double)project.CompletedTaskCount / project.TaskCount * 100)
                    : 0;
                return project;
            }

            public async Task<Guid?> GetTenantIdAsync(Guid id)
            {
                return await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .Select(p => (Guid?)p.TenantId)
                    .SingleOrDefaultAsync();
            }

            public async Task<Project?> GetByIdAsync(Guid Id)
            {
                return await _context.Projects
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .FirstOrDefaultAsync(p => p.Id == Id);
            }

            public async Task<bool> ExistsAsync(Guid Id)
            {
                return await _context.Projects
                    .AnyAsync(p => p.Id == Id && !p.IsDeleted);
            }

            public async Task<Project> AddAsync(Project project)
            {
                await _context.Projects.AddAsync(project);
                await _context.SaveChangesAsync();
                return project;
            }

            public async Task UpdateAsync(Project project)
            {
                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(Project project)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            public async Task<int> CountActiveProjectsByTenantAsync(Guid tenantId)
            {
                return await _context.Projects
                    .AsNoTracking()
                    .CountAsync(p => p.TenantId == tenantId && !p.IsDeleted);
            }
        }
    }