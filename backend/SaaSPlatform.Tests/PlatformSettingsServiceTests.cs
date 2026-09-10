using Moq;
using SaaSPlatform.Application.DTOS.Settings;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class PlatformSettingsServiceTests
    {
        private readonly Mock<IPlatformSettingsRepository> _repository = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly PlatformSettingsService _service;

        public PlatformSettingsServiceTests()
        {
            _service = new PlatformSettingsService(_repository.Object, _logs.Object);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsMappedDto()
        {
            var entity = new PlatformSetting
            {
                PlatformName = "Custom SaaS",
                SupportEmail = "admin@saas.com",
                MaintenanceMode = true,
                AllowRegistrations = false,
                MfaRequired = true,
                SessionTimeout = 45
            };
            _repository.Setup(x => x.GetSettingsAsync()).ReturnsAsync(entity);

            var result = await _service.GetSettingsAsync();

            Assert.Equal("Custom SaaS", result.PlatformName);
            Assert.Equal("admin@saas.com", result.SupportEmail);
            Assert.True(result.MaintenanceMode);
            Assert.False(result.AllowRegistrations);
            Assert.True(result.MfaRequired);
            Assert.Equal(45, result.SessionTimeout);
        }

        [Fact]
        public async Task UpdateSettingsAsync_UpdatesEntityAndLogsAudit()
        {
            var adminId = Guid.NewGuid();
            var dto = new UpdatePlatformSettingsDto
            {
                PlatformName = "Updated Name",
                SupportEmail = "support@updated.com",
                MaintenanceMode = false,
                AllowRegistrations = true,
                MfaRequired = false,
                SessionTimeout = 60
            };

            _repository.Setup(x => x.UpdateSettingsAsync(It.IsAny<PlatformSetting>()))
                .ReturnsAsync((PlatformSetting s) => s);

            var result = await _service.UpdateSettingsAsync(dto, adminId);

            Assert.Equal("Updated Name", result.PlatformName);
            Assert.Equal("support@updated.com", result.SupportEmail);
            Assert.Equal(60, result.SessionTimeout);
            _logs.Verify(
                x => x.LogAsync("SETTINGS_UPDATED", It.Is<string>(m => m.Contains("Updated Name")), adminId, null),
                Times.Once);
        }
    }
}
