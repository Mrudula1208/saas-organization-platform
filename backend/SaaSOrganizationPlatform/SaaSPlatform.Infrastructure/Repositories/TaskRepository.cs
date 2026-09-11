using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<TaskItem>> GetAllAsync(Guid tenantId)
        {
            return await _context.TaskItems
                .Include(t=>t.Project)
                .Where(t =>t.Project.TenantId == tenantId).ToListAsync();
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