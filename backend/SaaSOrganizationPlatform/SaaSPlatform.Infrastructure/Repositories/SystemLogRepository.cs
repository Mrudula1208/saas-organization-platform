using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.DTOS;
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
                // Keep a missing tenant as NULL for global/system events. It
                // preserves the nullable tenant/date lookup semantics.
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);
            // Save inside LogAsync because log calls might be immediate and independent of main business transaction saving
            await _context.SaveChangesAsync();
        }

        // Paged, filtered log list. tenantId = null means the super admin list (every tenant).
        // Filtering and paging happen in the database.
        public async Task<PagedResult<SystemLog>> GetLogsPage(Guid? tenantId, string? actionType, string? search, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.SystemLogs.AsNoTracking();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                query = query.Where(l => l.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(l => l.Action == actionType);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(l =>
                    l.Action.ToLower().Contains(lowerSearch) ||
                    l.Description.ToLower().Contains(lowerSearch));
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Include the whole day of the selected end date
                // (endDate often comes from a date input as midnight).
                query = query.Where(l => l.CreatedAt < endDate.Value.AddDays(1));
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                // Newest first; Id breaks ties so pages never skip or repeat a row.
                .OrderByDescending(l => l.CreatedAt)
                .ThenBy(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SystemLog>
            {
                Data = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
