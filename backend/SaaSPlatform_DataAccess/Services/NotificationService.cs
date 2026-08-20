using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<IEnumerable<Notification>> GetAllAsync(Guid tenantId)
        {
            return await _notificationRepository.GetAllAsync(tenantId);
        }

        public async Task<int> GetUnreadCountAsync(Guid tenantId)
        {
            return await _notificationRepository.GetUnreadCountAsync(tenantId);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _notificationRepository.GetByIdAsync(id);
        }

        public async Task<Notification> CreateAsync(Guid tenantId, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            return notification;
        }

        public async Task<bool> MarkReadAsync(Guid id, Guid tenantId)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null || notification.TenantId != tenantId)
                return false;

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
            return true;
        }

        public async Task<bool> MarkAllReadAsync(Guid tenantId)
        {
            await _notificationRepository.MarkAllReadAsync(tenantId);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid tenantId)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null || notification.TenantId != tenantId)
                return false;

            await _notificationRepository.DeleteAsync(notification);
            return true;
        }
    }
}
