using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITenantService
    {
        Task<IEnumerable<Tenant>> GetAllAsync();
        Task<Tenant?> GetByIdAsync(Guid Id);  
        Task<Tenant> CreateAsync(Tenant tenant);
        Task<bool> UpdateAsync(Guid Id, Tenant tenant);
        Task<bool> DeleteAsync(Guid Id);
        Task<bool> UpdateLogoAsync(Guid tenantId, string logoUrl);
    }
}
