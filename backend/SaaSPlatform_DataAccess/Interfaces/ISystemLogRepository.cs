using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ISystemLogRepository
    {
        Task LogAsync(string action, string message, Guid? userId, Guid? tenantId);

        // tenantId = null means the super admin list (logs of every tenant).
        // Filtering and paging happen in the database.
        Task<PagedResult<SystemLog>> GetLogsPage(Guid? tenantId, string? actionType, string? search, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    }
}
