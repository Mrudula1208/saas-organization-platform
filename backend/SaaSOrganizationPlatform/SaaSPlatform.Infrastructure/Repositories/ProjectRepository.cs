using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
    {
        public class ProjectRepository : IProjectRepository
        {
            private readonly ApplicationDbContext _context;

            public ProjectRepository(ApplicationDbContext context)
            {
                _context = context;
            }

            public async Task<IEnumerable<Project>> GetAllAsync(Guid tenantId)
            {
                return await _context.Projects
                    .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }

            public async Task<Project?> GetByIdAsync(Guid Id)
            {
                return await _context.Projects
                    .Include(p => p.Owner)
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .FirstOrDefaultAsync(p => p.Id == Id);
            }

            public async Task<bool> ExistsAsync(Guid Id)
            {
                return await _context.Projects
                    .AnyAsync(p => p.Id == Id && !p.IsDeleted);
            }

            public async Task<Project> AddAsync(Project project)
            {
                await _context.Projects.AddAsync(project);
                await _context.SaveChangesAsync();
                return project;
            }

            public async Task UpdateAsync(Project project)
            {
                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(Project project)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
        }
    }