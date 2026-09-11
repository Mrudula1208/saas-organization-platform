using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetAllAsync(Guid tenantId, Guid? projectId = null, string? status = null, string? search = null);
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task<TaskItem> CreateAsync(CreateTaskDto dto);
        Task UpdateAsync(Guid id, UpdateTaskDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> UpdateStatusAsync(Guid id, string status);
    }
}
