using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetAllAsync(Guid tenantId);
        Task<int> GetUnreadCountAsync(Guid tenantId);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<Notification> CreateAsync(Guid tenantId, string message);
        Task<bool> MarkReadAsync(Guid id, Guid tenantId);
        Task<bool> MarkAllReadAsync(Guid tenantId);
        Task<bool> DeleteAsync(Guid id, Guid tenantId);
    }
}
