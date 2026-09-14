using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tenants;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ITenantService
    {
        // One page of the tenant list. Filtering and paging happen in the database.
        Task<PagedResult<Tenant>> GetTenantsPage(string? search = null, string? plan = null, int page = 1, int pageSize = 20);
        Task<Tenant?> GetByIdAsync(Guid Id);  
        Task<Tenant> CreateAsync(Tenant tenant, Guid? userId = null);
        Task<bool> UpdateAsync(Guid Id, Tenant tenant, Guid? userId = null);
        Task<bool> DeleteAsync(Guid Id, Guid? userId = null);
        Task<bool> UpdateLogoAsync(Guid tenantId, string logoUrl, Guid? userId = null);
        Task<TenantSettingsDto?> GetSettingsAsync(Guid tenantId);
        Task<bool> UpdateSettingsAsync(Guid tenantId, TenantSettingsDto dto, Guid? userId = null);
    }
}
