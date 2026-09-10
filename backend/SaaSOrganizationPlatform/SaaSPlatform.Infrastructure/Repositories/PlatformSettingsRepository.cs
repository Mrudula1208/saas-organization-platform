using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class PlatformSettingsRepository : IPlatformSettingsRepository
    {
        private readonly ApplicationDbContext _context;

        public PlatformSettingsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlatformSetting> GetSettingsAsync()
        {
            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // Create default configuration row if table is empty
                settings = new PlatformSetting();
                await _context.PlatformSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public async Task<PlatformSetting> UpdateSettingsAsync(PlatformSetting settings)
        {
            var existing = await _context.PlatformSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                await _context.PlatformSettings.AddAsync(settings);
            }
            else
            {
                existing.PlatformName = settings.PlatformName;
                existing.SupportEmail = settings.SupportEmail;
                existing.MaintenanceMode = settings.MaintenanceMode;
                existing.AllowRegistrations = settings.AllowRegistrations;
                existing.MfaRequired = settings.MfaRequired;
                existing.SessionTimeout = settings.SessionTimeout;
                existing.UpdatedAt = settings.UpdatedAt;
            }

            await _context.SaveChangesAsync();
            return existing ?? settings;
        }
    }
}
