using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITaskRepository
    {
        // One paged query for a single tenant. Filtering and paging happen in the database.
        Task<PagedResult<TaskItem>> GetTasksPage(Guid tenantId, Guid? projectId, string? status, string? search, int page, int pageSize);
        Task<TaskItem>GetByIdAsync(Guid Id);
        Task<TaskItem>AddAsync(TaskItem task);

        Task UpdateAsync(TaskItem task);
        Task DeleteAsync(TaskItem task);
    }
}
