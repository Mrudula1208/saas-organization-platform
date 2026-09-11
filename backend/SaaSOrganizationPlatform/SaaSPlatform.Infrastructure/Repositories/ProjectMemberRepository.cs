using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class ProjectMemberRepository : IProjectMemberRepository
    {
        private readonly ApplicationDbContext _context;
        public ProjectMemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectMember>>  GetMembersAsync(Guid tenantId)
        {
            return await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm=>pm.Project)
                //ProjectMember doesn’t have TenantId
                // So we use Project
                .Where(pm=>pm.Project.TenantId==tenantId)
                .ToListAsync();
        }

        public async Task <ProjectMember>AddAsync(ProjectMember member)
        {
            await _context.ProjectMembers.AddAsync(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task<ProjectMember>GetByIdAsync(Guid Id)
        {
            return await _context.ProjectMembers.FindAsync(Id);
        }


        public  async Task DeleteAsync(ProjectMember member)
        {
           _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}
