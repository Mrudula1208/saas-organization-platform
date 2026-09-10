using SaaSPlatform.Application.DTOS.Settings;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IPlatformSettingsService
    {
        Task<PlatformSettingsDto> GetSettingsAsync();
        Task<PlatformSettingsDto> UpdateSettingsAsync(UpdatePlatformSettingsDto dto, Guid? adminUserId);
    }
}
