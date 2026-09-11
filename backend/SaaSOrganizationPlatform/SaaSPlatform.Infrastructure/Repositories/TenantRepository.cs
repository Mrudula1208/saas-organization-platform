using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model.Entities;
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

        public async Task<IEnumerable<Tenant>> GetAllAsync()
        {
            return await _context.Tenants.Where(t => !t.IsDeleted).ToListAsync();
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
