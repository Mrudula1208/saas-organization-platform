using SaaSPlatform.Application.DTOS.Settings;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class PlatformSettingsService : IPlatformSettingsService
    {
        private readonly IPlatformSettingsRepository _repository;
        private readonly ISystemLogRepository _systemLogs;

        public PlatformSettingsService(
            IPlatformSettingsRepository repository,
            ISystemLogRepository systemLogs)
        {
            _repository = repository;
            _systemLogs = systemLogs;
        }

        public async Task<PlatformSettingsDto> GetSettingsAsync()
        {
            var entity = await _repository.GetSettingsAsync();
            return MapToDto(entity);
        }

        public async Task<PlatformSettingsDto> UpdateSettingsAsync(UpdatePlatformSettingsDto dto, Guid? adminUserId)
        {
            var entity = new PlatformSetting
            {
                PlatformName = dto.PlatformName.Trim(),
                SupportEmail = dto.SupportEmail.Trim().ToLower(),
                MaintenanceMode = dto.MaintenanceMode,
                AllowRegistrations = dto.AllowRegistrations,
                MfaRequired = dto.MfaRequired,
                SessionTimeout = dto.SessionTimeout,
                UpdatedAt = DateTime.UtcNow
            };

            var updated = await _repository.UpdateSettingsAsync(entity);

            // Audit the settings change in the system log
            await _systemLogs.LogAsync(
                "SETTINGS_UPDATED",
                $"Platform configuration updated (PlatformName: {updated.PlatformName}, MaintenanceMode: {updated.MaintenanceMode}, AllowRegistrations: {updated.AllowRegistrations}).",
                adminUserId,
                null);

            return MapToDto(updated);
        }

        private static PlatformSettingsDto MapToDto(PlatformSetting entity)
        {
            return new PlatformSettingsDto
            {
                PlatformName = entity.PlatformName,
                SupportEmail = entity.SupportEmail,
                MaintenanceMode = entity.MaintenanceMode,
                AllowRegistrations = entity.AllowRegistrations,
                MfaRequired = entity.MfaRequired,
                SessionTimeout = entity.SessionTimeout,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
