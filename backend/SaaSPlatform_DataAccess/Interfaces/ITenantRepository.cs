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
        Task<IEnumerable<Tenant>> GetAllAsync();
        Task<Tenant?> GetByIdAsync(Guid Id);
        Task<Tenant> AddAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
        Task DeleteAsync(Tenant tenant);

    }
}
