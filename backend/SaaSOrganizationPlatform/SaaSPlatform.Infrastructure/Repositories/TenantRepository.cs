using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model.Entities;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;

        public TenantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Paged, filtered tenant list. Filtering and paging happen in the database.
        public async Task<PagedResult<Tenant>> GetTenantsPage(string? search, string? plan, int page, int pageSize)
        {
            var query = _context.Tenants.AsNoTracking().Where(t => !t.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(lowerSearch) ||
                    t.Domain.ToLower().Contains(lowerSearch) ||
                    t.ContactEmail.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(plan))
            {
                // Keep the plan filter in the same SQL statement rather than
                // materialising a list of plan ids first.
                var lowerPlan = plan.ToLower();
                query = query.Where(t => _context.SubscriptionPlans
                    .Any(p => p.Id == t.SubscriptionPlanId && p.Name.ToLower() == lowerPlan));
            }

            var totalCount = await query.CountAsync();

            var tenants = await query
                // Newest first; Id breaks ties so pages never skip or repeat a row.
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Tenant>
            {
                Data = tenants,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Tenant?> GetByIdAsync(Guid Id)
        {
            return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == Id && !t.IsDeleted);
        }

        public async Task<Tenant> AddAsync(Tenant tenant)
        {
            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }


        public async Task  UpdateAsync(Tenant tenant)
        {
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Tenant tenant)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync(); 
        }
    }
}
