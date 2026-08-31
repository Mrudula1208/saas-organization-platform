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
    public class ProjectMemberRepository : IProjectMemberRepository
    {
        private readonly ApplicationDbContext _context;
        public ProjectMemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(Guid projectId, Guid tenantId)
        {
            return await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .Where(pm => pm.ProjectId == projectId && pm.Project.TenantId == tenantId)
                .OrderBy(pm => pm.User.FullName)
                .ToListAsync();
        }

        public async Task<ProjectMember?> GetByIdAsync(Guid id)
        {
            return await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.Id == id);
        }

        public async Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId)
        {
            return await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }

        public async Task<ProjectMember> AddAsync(ProjectMember member)
        {
            var existing = await GetByProjectAndUserAsync(member.ProjectId, member.UserId);
            if (existing != null)
            {
                throw new InvalidOperationException("User is already a member of this project.");
            }

            await _context.ProjectMembers.AddAsync(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task DeleteAsync(ProjectMember member)
        {
            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}