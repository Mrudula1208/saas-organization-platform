using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITaskService
    {
        // One page of the tenant task list. Filtering and paging happen in the database.
        Task<PagedResult<TaskItem>> GetTasksPage(Guid tenantId, Guid? projectId = null, string? status = null, string? search = null, int page = 1, int pageSize = 20);
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task<TaskItem> CreateAsync(CreateTaskDto dto);
        Task UpdateAsync(Guid id, UpdateTaskDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> UpdateStatusAsync(Guid id, string status);
    }
}
