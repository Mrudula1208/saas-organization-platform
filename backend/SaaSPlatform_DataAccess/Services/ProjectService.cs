using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ISystemLogRepository _systemLogs;

        public ProjectService(IProjectRepository projectRepository, ISystemLogRepository systemLogs)
        {
            _projectRepository = projectRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<Project>> GetAllAsync(Guid tenantId, string? search = null, string? status = null, string? priority = null)
        {
            var projects = await _projectRepository.GetAllAsync(tenantId);
            var query = projects.AsQueryable();

            // Filter out soft-deleted projects
            query = query.Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerSearch) || p.Description.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(p => p.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        public async Task<Project?> GetByIdAsync(Guid Id)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted) return null;
            return project;
        }

        public async Task<Project> CreateAsync(CreateProjectDto dto)
        {
            if (string.IsNullOrEmpty(dto.Name))
            {
                throw new Exception("Project name is required.");
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                TenantId = dto.TenantId,
                OwnerId = dto.OwnerId,
                Status = dto.Status ?? "Backlog",
                Priority = dto.Priority ?? "Medium",
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate ?? DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdProject = await _projectRepository.AddAsync(project);
            await _systemLogs.LogAsync("PROJECT_CREATED", $"Project {createdProject.Name} created.", createdProject.OwnerId, createdProject.TenantId);
            return createdProject;
        }

        public async Task UpdateAsync(Guid Id, UpdateProjectDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted)
            {
                throw new Exception("Project not found.");
            }

            project.Name = dto.Name;
            project.Description = dto.Description ?? string.Empty;
            project.Status = dto.Status ?? project.Status;
            project.Priority = dto.Priority ?? project.Priority;
            project.StartDate = dto.StartDate ?? project.StartDate;
            project.EndDate = dto.EndDate ?? project.EndDate;
            project.IsActive = dto.IsActive;

            await _projectRepository.UpdateAsync(project);
            await _systemLogs.LogAsync("PROJECT_UPDATED", $"Project {project.Name} updated.", project.OwnerId, project.TenantId);
        }

        public async Task DeleteAsync(Guid Id)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted)
            {
                throw new Exception("Project not found.");
            }

            project.IsDeleted = true;
            await _projectRepository.UpdateAsync(project);
            await _systemLogs.LogAsync("PROJECT_DELETED", $"Project {project.Name} soft deleted.", project.OwnerId, project.TenantId);
        }
    }
}
