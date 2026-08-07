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
    public class SystemLogRepository : ISystemLogRepository
    {
        private readonly ApplicationDbContext _context;

        public SystemLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string message, Guid? userId, Guid? tenantId)
        {
            var log = new SystemLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                Description = message,
                UserId = userId,
                TenantId = tenantId ?? Guid.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);
            // Save inside LogAsync because log calls might be immediate and independent of main business transaction saving
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetAllAsync(Guid? tenantId, string? actionType, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.SystemLogs.AsQueryable();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                query = query.Where(l => l.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(l => l.Action == actionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            return await query.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();
        }
    }
}
