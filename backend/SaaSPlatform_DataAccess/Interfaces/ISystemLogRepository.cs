using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ISystemLogRepository
    {
        Task LogAsync(string action, string message, Guid? userId, Guid? tenantId);
        Task<IEnumerable<SystemLog>> GetAllAsync(Guid? tenantId, string? actionType, DateTime? startDate, DateTime? endDate);
    }
}
