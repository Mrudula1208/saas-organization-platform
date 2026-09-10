using SaaSPlatform.Domain.Entities;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IPlatformSettingsRepository
    {
        Task<PlatformSetting> GetSettingsAsync();
        Task<PlatformSetting> UpdateSettingsAsync(PlatformSetting settings);
    }
}
