using SaaSPlatform.Application.DTOS;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITenantRepository
    {
        // One paged query. Filtering and paging happen in the database.
        Task<PagedResult<Tenant>> GetTenantsPage(string? search, string? plan, int page, int pageSize);
        Task<Tenant?> GetByIdAsync(Guid Id);
        Task<Tenant> AddAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
        Task DeleteAsync(Tenant tenant);

    }
}
