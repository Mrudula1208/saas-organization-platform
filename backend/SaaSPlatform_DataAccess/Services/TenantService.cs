using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ISystemLogRepository _systemLogs;

        public TenantService(ITenantRepository tenantRepository, ISystemLogRepository systemLogs)
        {
            _tenantRepository = tenantRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<Tenant>> GetAllAsync()
        {
            return await _tenantRepository.GetAllAsync();
        }

        public async Task<Tenant?> GetByIdAsync(Guid Id)
        {
            return await _tenantRepository.GetByIdAsync(Id);
        }

        public async Task<Tenant> CreateAsync(Tenant tenant)
        {
            if (string.IsNullOrEmpty(tenant.Name))
            {
                throw new Exception("Tenant name is Required.");
            }

            tenant.CreatedAt = DateTime.UtcNow;
            tenant.IsActive = true;
            tenant.IsDeleted = false;

            var createdTenant = await _tenantRepository.AddAsync(tenant);
            await _systemLogs.LogAsync("TENANT_CREATED", $"Tenant {createdTenant.Name} created.", null, createdTenant.Id);
            return createdTenant;
        }

        public async Task<bool> UpdateAsync(Guid Id, Tenant tenant)
        {
            var existingTenant = await _tenantRepository.GetByIdAsync(Id);
            if (existingTenant == null || existingTenant.IsDeleted)
            {
                return false;
            }

            existingTenant.Name = tenant.Name;
            existingTenant.ContactEmail = tenant.ContactEmail;
            existingTenant.ContactPhone = tenant.ContactPhone;
            existingTenant.IsActive = tenant.IsActive;

            await _tenantRepository.UpdateAsync(existingTenant);
            await _systemLogs.LogAsync("TENANT_UPDATED", $"Tenant {existingTenant.Name} profile details updated.", null, existingTenant.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid Id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(Id);
            if (tenant == null || tenant.IsDeleted)
            {
                return false;
            }

            tenant.IsDeleted = true;
            await _tenantRepository.UpdateAsync(tenant);
            await _systemLogs.LogAsync("TENANT_DELETED", $"Tenant {tenant.Name} soft deleted.", null, tenant.Id);
            return true;
        }

        public async Task<bool> UpdateLogoAsync(Guid tenantId, string logoUrl)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return false;
            }

            tenant.LogoImageUrl = logoUrl;
            await _tenantRepository.UpdateAsync(tenant);
            await _systemLogs.LogAsync("TENANT_LOGO_UPDATED", $"Tenant {tenant.Name} company logo updated.", null, tenant.Id);
            return true;
        }
    }
}