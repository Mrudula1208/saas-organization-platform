using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetAllAsync(Guid tenantId)
        {
            return await _context.Notifications
                .Where(n => n.TenantId == tenantId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid tenantId)
        {
            return await _context.Notifications
                .CountAsync(n => n.TenantId == tenantId && !n.IsRead);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync(Guid tenantId)
        {
            var unread = await _context.Notifications
                .Where(n => n.TenantId == tenantId && !n.IsRead)
                .ToListAsync();

            foreach (var notif in unread)
            {
                notif.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
