using SaaSPlatform.Application.DTOS;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IUserRepository
    {
        // One paged query used by both lists. tenantId = null means the super admin list (every tenant).
        // Filtering and paging happen in the database.
        Task<PagedResult<User>> GetUsersPage(Guid? tenantId, string? search, string? role, bool? isActive, int page, int pageSize);

        Task<User?>GetUserById(Guid Id);

        Task<User> CreateUser(User user);   

        Task<bool> UpdateUser(Guid Id ,User user);
        Task <bool>DeleteUser(Guid Id);
        Task<User?>GetByEmailAsync (string  Email);
        Task<int> CountActiveUsersByTenantAsync(Guid tenantId);
    }
}
