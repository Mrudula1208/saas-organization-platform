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
        Task<IEnumerable<TaskItem>>GetAllAsync(Guid tenantId);
        Task<TaskItem>GetByIdAsync(Guid Id);
        Task<TaskItem>AddAsync(TaskItem task);

        Task UpdateAsync(TaskItem task);
        Task DeleteAsync(TaskItem task);
    }
}
