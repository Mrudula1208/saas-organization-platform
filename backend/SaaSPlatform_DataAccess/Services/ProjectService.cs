using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ProjectService : IProjectService
    {
        private static readonly string[] AllowedStatuses =
            { "Backlog", "Pending", "In Progress", "Active", "On Hold", "Completed", "Cancelled" };

        private static readonly string[] AllowedPriorities = { "Low", "Medium", "High" };

        private readonly IProjectRepository _projectRepository;
        private readonly ISystemLogRepository _systemLogs;

        public ProjectService(IProjectRepository projectRepository, ISystemLogRepository systemLogs)
        {
            _projectRepository = projectRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<ProjectViewDto>> GetAllAsync(Guid tenantId, string? search = null, string? status = null, string? priority = null)
        {
            var projects = await _projectRepository.GetAllAsync(tenantId);
            var query = projects.Where(p => !p.IsDeleted).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p =>
                    (p.Name ?? string.Empty).ToLower().Contains(lowerSearch) ||
                    (p.Description ?? string.Empty).ToLower().Contains(lowerSearch)).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => (p.Status ?? string.Empty).Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(p => (p.Priority ?? string.Empty).Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return query.Select(MapToDto).ToList();
        }

        public async Task<ProjectViewDto?> GetByIdAsync(Guid Id, Guid tenantId)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted) return null;

            // Never expose another tenant's project, regardless of what the caller sends.
            if (project.TenantId != tenantId) return null;

            return MapToDto(project);
        }

        public async Task<bool> ExistsAsync(Guid Id)
        {
            return await _projectRepository.ExistsAsync(Id);
        }

        public async Task<ProjectViewDto> CreateAsync(CreateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Project name is required.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                !AllowedStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid project status '{dto.Status}'.");
            }

            if (dto.StartDate != default && dto.EndDate != default && dto.EndDate < dto.StartDate)
            {
                throw new ArgumentException("End date cannot be before the start date.");
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                TenantId = dto.TenantId,
                OwnerId = dto.OwnerId,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending" : dto.Status,
                Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdProject = await _projectRepository.AddAsync(project);
            await _systemLogs.LogAsync("PROJECT_CREATED", $"Project {createdProject.Name} created.", createdProject.OwnerId, createdProject.TenantId);
            return MapToDto(createdProject);
        }

        public async Task UpdateAsync(Guid Id, Guid tenantId, UpdateProjectDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted)
            {
                throw new KeyNotFoundException("Project not found.");
            }

            // Tenant isolation: the tenant always comes from the caller's token,
            // never from the request body.
            if (project.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("You do not have access to this project.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Project name is required.");
            }
            if (dto.Name.Length > 200)
            {
                throw new ArgumentException("Project name must be 200 characters or fewer.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                !AllowedStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid project status '{dto.Status}'.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Priority) &&
                !AllowedPriorities.Contains(dto.Priority, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid project priority '{dto.Priority}'.");
            }

            if (dto.StartDate != default && dto.EndDate != default && dto.EndDate < dto.StartDate)
            {
                throw new ArgumentException("End date cannot be before the start date.");
            }

            project.Name = dto.Name.Trim();
            project.Description = dto.Description ?? string.Empty;
            project.Status = string.IsNullOrWhiteSpace(dto.Status) ? project.Status : dto.Status;
            project.Priority = string.IsNullOrWhiteSpace(dto.Priority) ? project.Priority : dto.Priority;
            project.StartDate = dto.StartDate == default ? project.StartDate : dto.StartDate;
            project.EndDate = dto.EndDate == default ? project.EndDate : dto.EndDate;
            project.IsActive = dto.IsActive;

            await _projectRepository.UpdateAsync(project);
            await _systemLogs.LogAsync("PROJECT_UPDATED", $"Project {project.Name} updated.", project.OwnerId, project.TenantId);
        }

        public async Task DeleteAsync(Guid Id, Guid tenantId)
        {
            var project = await _projectRepository.GetByIdAsync(Id);
            if (project == null || project.IsDeleted)
            {
                throw new KeyNotFoundException("Project not found.");
            }

            // Tenant isolation: only the owning tenant may delete the project.
            if (project.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("You do not have access to this project.");
            }

            project.IsDeleted = true;
            project.IsActive = false;
            await _projectRepository.UpdateAsync(project);
            await _systemLogs.LogAsync("PROJECT_DELETED", $"Project {project.Name} soft deleted.", project.OwnerId, project.TenantId);
        }

        private static ProjectViewDto MapToDto(Project project)
        {
            var taskList = (project.Tasks ?? Array.Empty<TaskItem>())
                .Where(t => !t.IsDeleted)
                .ToList();

            var completed = taskList.Count(t =>
                t.IsCompleted || (t.Status ?? string.Empty).Equals("Completed", StringComparison.OrdinalIgnoreCase));

            return new ProjectViewDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description ?? string.Empty,
                TenantId = project.TenantId,
                OwnerId = project.OwnerId,
                OwnerName = project.Owner?.FullName ?? string.Empty,
                Status = project.Status ?? "Pending",
                Priority = project.Priority ?? "Medium",
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                TaskCount = taskList.Count,
                CompletedTaskCount = completed,
                Progress = taskList.Count > 0 ? (int)Math.Round((double)completed / taskList.Count * 100) : 0
            };
        }
    }
}