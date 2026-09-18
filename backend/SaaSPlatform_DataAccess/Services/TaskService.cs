using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ISystemLogRepository _systemLogs;
        private readonly IUserRepository? _userRepository;

        public TaskService(
            ITaskRepository taskRepository,
            IProjectRepository projectRepository,
            ISystemLogRepository systemLogs,
            IUserRepository? userRepository = null)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _systemLogs = systemLogs;
            _userRepository = userRepository;
        }

        // One page of the tenant task list; the database does the filtering and paging.
        public async Task<PagedResult<TaskItem>> GetTasksPage(Guid tenantId, Guid? projectId = null, string? status = null, string? search = null, int page = 1, int pageSize = 20)
        {
            var result = await _taskRepository.GetTasksPage(tenantId, projectId, status, search, page, pageSize);

            // Break the JSON reference cycle: EF navigation fix-up fills
            // Project.Tasks with the loaded tasks, and that cycles back here.
            foreach (var task in result.Data)
            {
                if (task.Project != null)
                {
                    task.Project.Tasks = null!;
                }

                if (task.AssignedUser != null)
                {
                    task.AssignedUser.AssignedTasks = null!;
                }
            }

            return result;
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null || task.IsDeleted) return null;
            return task;
        }

        public async Task<TaskItem> CreateAsync(CreateTaskDto dto)
        {
            if (string.IsNullOrEmpty(dto.Name))
            {
                throw new Exception("Task name is required.");
            }

            var projectTenantId = await _projectRepository.GetTenantIdAsync(dto.ProjectId);
            if (projectTenantId.HasValue)
            {
                if (projectTenantId.Value != dto.TenantId)
                    throw new Exception("Selected project was not found in this tenant.");
            }
            else
            {
                // Compatibility path for older repository implementations.
                var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (project == null || project.IsDeleted || project.TenantId != dto.TenantId)
                    throw new Exception("Selected project was not found in this tenant.");
            }

            if (dto.AssignedUserId != Guid.Empty && _userRepository != null)
            {
                var assignedUser = await _userRepository.GetUserById(dto.AssignedUserId);
                if (assignedUser == null || assignedUser.IsDeleted || assignedUser.TenantId != dto.TenantId)
                {
                    throw new Exception("Assigned user was not found in this tenant.");
                }
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                ProjectId = dto.ProjectId,
                AssignedUserId = dto.AssignedUserId,
                Status = dto.Status ?? "To Do",
                Priority = dto.Priority ?? "Medium",
                DueDate = dto.DueDate,
                IsCompleted = false,
                IsDeleted = false,
                TenantId = dto.TenantId,
                CreatedAt = DateTime.UtcNow
            };

            var createdTask = await _taskRepository.AddAsync(task);
            await _systemLogs.LogAsync("TASK_CREATED", $"Task {createdTask.Name} created in Project {createdTask.ProjectId}.", createdTask.AssignedUserId, createdTask.TenantId);
            return createdTask;
        }

        public async Task UpdateAsync(Guid id, UpdateTaskDto dto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null || task.IsDeleted)
            {
                throw new Exception("Task not found.");
            }

            if (dto.AssignedUserId != Guid.Empty && _userRepository != null)
            {
                var assignedUser = await _userRepository.GetUserById(dto.AssignedUserId);
                if (assignedUser == null || assignedUser.IsDeleted || assignedUser.TenantId != task.TenantId)
                {
                    throw new Exception("Assigned user was not found in this tenant.");
                }
            }

            task.Name = dto.Name;
            task.Description = dto.Description ?? string.Empty;
            task.AssignedUserId = dto.AssignedUserId;
            task.Status = dto.Status ?? task.Status;
            task.Priority = dto.Priority ?? task.Priority;
            task.DueDate = dto.DueDate;
            task.IsCompleted = dto.IsCompleted;

            if (task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase))
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
            }

            await _taskRepository.UpdateAsync(task);
            await _systemLogs.LogAsync("TASK_UPDATED", $"Task {task.Name} details updated.", task.AssignedUserId, task.TenantId);
        }

        public async Task DeleteAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null || task.IsDeleted)
            {
                throw new Exception("Task not found.");
            }

            task.IsDeleted = true;
            await _taskRepository.UpdateAsync(task);
            await _systemLogs.LogAsync("TASK_DELETED", $"Task {task.Name} soft deleted.", task.AssignedUserId, task.TenantId);
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null || task.IsDeleted)
            {
                return false;
            }

            var oldStatus = task.Status;
            task.Status = status;

            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || status.Equals("Done", StringComparison.OrdinalIgnoreCase))
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
            }

            await _taskRepository.UpdateAsync(task);
            await _systemLogs.LogAsync("TASK_STATUS_UPDATED", $"Task '{task.Name}' moved from '{oldStatus}' to '{status}'.", task.AssignedUserId, task.TenantId);
            return true;
        }
    }
}