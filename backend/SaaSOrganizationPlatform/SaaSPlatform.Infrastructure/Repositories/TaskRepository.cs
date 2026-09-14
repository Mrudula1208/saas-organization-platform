using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Paged, filtered task list for one tenant. Filtering and paging happen in the database.
        public async Task<PagedResult<TaskItem>> GetTasksPage(Guid tenantId, Guid? projectId, string? status, string? search, int page, int pageSize)
        {
            var query = _context.TaskItems.AsNoTracking()
                // TaskItem carries the tenant key, so this avoids a join through
                // Project and can use the tenant/status index.
                .Where(t => t.TenantId == tenantId && !t.IsDeleted);

            if (projectId.HasValue && projectId.Value != Guid.Empty)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(t => (t.Status ?? string.Empty).ToLower() == lowerStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(lowerSearch) ||
                    t.Description.ToLower().Contains(lowerSearch));
            }

            var totalCount = await query.CountAsync();

            var tasks = await query
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                // Newest first; Id breaks ties so pages never skip or repeat a row.
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TaskItem>
            {
                Data = tasks,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<TaskItem?> GetByIdAsync(Guid Id)
        {
            return await _context.TaskItems.FindAsync(Id);
        }


        public async Task<TaskItem> AddAsync(TaskItem task)
        {
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task UpdateAsync(TaskItem task)
        {
            _context.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TaskItem task)
        {
            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
        }

    }
}