using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ISystemLogRepository _systemLogs;

        public TaskService(ITaskRepository taskRepository, ISystemLogRepository systemLogs)
        {
            _taskRepository = taskRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync(Guid tenantId, Guid? projectId = null, string? status = null, string? search = null)
        {
            var tasks = await _taskRepository.GetAllAsync(tenantId);
            var query = tasks.AsQueryable();

            // Filter out soft-deleted tasks
            query = query.Where(t => !t.IsDeleted);

            if (projectId.HasValue && projectId.Value != Guid.Empty)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(lowerSearch) || t.Description.ToLower().Contains(lowerSearch));
            }

            return query.ToList();
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
            task.IsCompleted = status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || status.Equals("Done", StringComparison.OrdinalIgnoreCase);

            await _taskRepository.UpdateAsync(task);
            await _systemLogs.LogAsync("TASK_STATUS_UPDATED", $"Task '{task.Name}' moved from '{oldStatus}' to '{status}'.", task.AssignedUserId, task.TenantId);
            return true;
        }
    }
}