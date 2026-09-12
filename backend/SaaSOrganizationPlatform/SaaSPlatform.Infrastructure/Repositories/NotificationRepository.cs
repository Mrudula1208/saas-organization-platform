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
                .AsNoTracking()
                .Where(n => n.TenantId == tenantId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
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
            // Set-based update: mark the matching rows in SQL instead of
            // materialising every unread notification and saving them one by one.
            await _context.Notifications
                .Where(n => n.TenantId == tenantId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
        }

        public async Task ClearAllAsync(Guid tenantId)
        {
            // Fast set-based deletion for tenant notifications
            await _context.Notifications
                .Where(n => n.TenantId == tenantId)
                .ExecuteDeleteAsync();
        }
    }
}
