using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tenants;
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

        // One page of the tenant list; the database does the filtering and paging.
        public async Task<PagedResult<Tenant>> GetTenantsPage(string? search = null, string? plan = null, int page = 1, int pageSize = 20)
        {
            return await _tenantRepository.GetTenantsPage(search, plan, page, pageSize);
        }

        public async Task<Tenant?> GetByIdAsync(Guid Id)
        {
            return await _tenantRepository.GetByIdAsync(Id);
        }

        public async Task<Tenant> CreateAsync(Tenant tenant, Guid? userId = null)
        {
            if (string.IsNullOrEmpty(tenant.Name))
            {
                throw new Exception("Tenant name is Required.");
            }

            tenant.CreatedAt = DateTime.UtcNow;
            tenant.IsActive = true;
            tenant.IsDeleted = false;

            var createdTenant = await _tenantRepository.AddAsync(tenant);
            await _systemLogs.LogAsync("TENANT_CREATED", $"Tenant {createdTenant.Name} created.", userId, createdTenant.Id);
            return createdTenant;
        }

        public async Task<bool> UpdateAsync(Guid Id, Tenant tenant, Guid? userId = null)
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
            await _systemLogs.LogAsync("TENANT_UPDATED", $"Tenant {existingTenant.Name} profile details updated.", userId, existingTenant.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid Id, Guid? userId = null)
        {
            var tenant = await _tenantRepository.GetByIdAsync(Id);
            if (tenant == null || tenant.IsDeleted)
            {
                return false;
            }

            tenant.IsDeleted = true;
            await _tenantRepository.UpdateAsync(tenant);
            await _systemLogs.LogAsync("TENANT_DELETED", $"Tenant {tenant.Name} soft deleted.", userId, tenant.Id);
            return true;
        }

        public async Task<bool> UpdateLogoAsync(Guid tenantId, string logoUrl, Guid? userId = null)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return false;
            }

            tenant.LogoImageUrl = logoUrl;
            await _tenantRepository.UpdateAsync(tenant);
            await _systemLogs.LogAsync("TENANT_LOGO_UPDATED", $"Tenant {tenant.Name} company logo updated.", userId, tenant.Id);
            return true;
        }

        public async Task<TenantSettingsDto?> GetSettingsAsync(Guid tenantId)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return null;
            }

            return new TenantSettingsDto
            {
                Id = tenant.Id,
                Name = tenant.Name ?? string.Empty,
                Domain = tenant.Domain ?? string.Empty,
                ContactEmail = tenant.ContactEmail ?? string.Empty,
                ContactPhone = tenant.ContactPhone ?? string.Empty,
                LogoImageUrl = tenant.LogoImageUrl,
                EmailNotifications = tenant.EmailNotificationsEnabled,
                InAppNotifications = tenant.InAppNotificationsEnabled
            };
        }

        public async Task<bool> UpdateSettingsAsync(Guid tenantId, TenantSettingsDto dto, Guid? userId = null)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Workspace name is required.");
            }

            // Only allowed settings fields are applied. Id/Domain/LogoImageUrl from the
            // request body are never trusted, and the tenant id comes exclusively from the caller.
            tenant.Name = dto.Name.Trim();
            tenant.ContactEmail = (dto.ContactEmail ?? string.Empty).Trim();
            tenant.ContactPhone = (dto.ContactPhone ?? string.Empty).Trim();
            tenant.EmailNotificationsEnabled = dto.EmailNotifications;
            tenant.InAppNotificationsEnabled = dto.InAppNotifications;

            await _tenantRepository.UpdateAsync(tenant);
            await _systemLogs.LogAsync("TENANT_SETTINGS_UPDATED", $"Tenant {tenant.Name} settings updated.", userId, tenant.Id);
            return true;
        }
    }
}
