using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
    {
        public class ProjectRepository : IProjectRepository
        {
            // DbContext = connection to database
            private readonly ApplicationDbContext _context;

            // Constructor Injection
            public ProjectRepository(ApplicationDbContext context)
            {
                _context = context;
            }

           
            public async Task<IEnumerable<Project>> GetAllAsync(Guid tenantId)
            {
            // Fetch all projects from database
            //+filter data exeutr sql query
            return await _context.Projects.Where(p => p.TenantId == tenantId).ToListAsync();
            }

            
            public async Task<Project?> GetByIdAsync(Guid Id)
            {
                // Find project using primary key
                return await _context.Projects.FindAsync(Id);
            }

            public async Task<Project> AddAsync(Project project)
            {
               
                await _context.Projects.AddAsync(project);

                await _context.SaveChangesAsync();

                return project;
            }

        
            public async Task UpdateAsync(Project project)
            {
                // Mark entity as modified
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
