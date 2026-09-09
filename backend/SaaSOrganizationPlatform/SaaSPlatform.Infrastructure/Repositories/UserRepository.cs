using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Infrastructure.Data;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class UserRepository:IUserRepository
    {

        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context =context;
        }

        // Paged, filtered user list. Filtering and paging happen in the database,
        // so the server never loads every user into memory.
        public async Task<PagedResult<User>> GetUsersPage(Guid? tenantId, string? search, string? role, bool? isActive, int page, int pageSize)
        {
            var query = _context.Users.AsNoTracking().Where(u => !u.IsDeleted);

            // Tenant users only ever see their own tenant (tenantId = null is the super admin list).
            if (tenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.FullName.ToLower().Contains(lowerSearch) || u.Email.ToLower().Contains(lowerSearch));
                }
                else
                {
                    // The super admin search can also match the tenant name.
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(lowerSearch) ||
                        u.Email.ToLower().Contains(lowerSearch) ||
                        (u.Tenant != null && u.Tenant.Name.ToLower().Contains(lowerSearch)));
                }
            }

            if (!string.IsNullOrEmpty(role))
            {
                var lowerRole = role.ToLower();
                query = query.Where(u => u.Role.ToLower() == lowerRole);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            // The tenant-scoped list only needs user fields. Loading the
            // inverse Tenant.Users graph here is unnecessary and creates a
            // serialization cycle; the platform list still needs Tenant.Name.
            if (!tenantId.HasValue)
            {
                query = query.Include(u => u.Tenant);
            }

            var users = await query
                // Newest first; Id breaks ties so pages never skip or repeat a row.
                .OrderByDescending(u => u.CreatedAt)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!tenantId.HasValue)
            {
                // Keep Tenant.Name for the platform table, but do not serialize
                // the inverse Tenant.Users collection back through every row.
                foreach (var user in users)
                {
                    if (user.Tenant != null)
                    {
                        user.Tenant.Users = null!;
                    }
                }
            }

            return new PagedResult<User>
            {
                Data = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }



        public async Task<User?> GetUserById(Guid Id)
        {
            return await _context.Users.FindAsync(Id);
        }


       


        public async Task<User> CreateUser(User user)
        {
             _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }


        public async Task<bool> UpdateUser ( Guid Id,User user)
        {
            var existingUser = await _context.Users.FindAsync(Id);
            if (existingUser == null)
                return false;
            
            if (!ReferenceEquals(existingUser, user))
            {
                _context.Entry(existingUser).CurrentValues.SetValues(user);
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteUser(Guid Id)
        {
            var user = await _context.Users.FindAsync(Id);
            if (user == null)
             return false;

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<User?> GetByEmailAsync(string Email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
        }

        public async Task<int> CountActiveUsersByTenantAsync(Guid tenantId)
        {
            return await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
        }
    }
}
