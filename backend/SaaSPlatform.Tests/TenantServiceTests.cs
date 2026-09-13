using Moq;
using SaaSPlatform.Application.DTOS.Tenants;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform_Model.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class TenantServiceTests
    {
        private readonly Mock<ITenantRepository> _tenants = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly TenantService _service;

        public TenantServiceTests()
        {
            _service = new TenantService(_tenants.Object, _logs.Object);
        }

        [Fact]
        public async Task CreateAsync_MissingName_Throws()
        {
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new Tenant { Name = "" }));

            Assert.Equal("Tenant name is Required.", ex.Message);
            _tenants.Verify(x => x.AddAsync(It.IsAny<Tenant>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidTenant_SetsDefaultsAndAudits()
        {
            _tenants.Setup(x => x.AddAsync(It.IsAny<Tenant>()))
                .ReturnsAsync((Tenant t) => t);

            var created = await _service.CreateAsync(new Tenant { Name = "Acme" });

            Assert.True(created.IsActive);
            Assert.False(created.IsDeleted);
            Assert.True(created.CreatedAt <= DateTime.UtcNow);
            _logs.Verify(
                x => x.LogAsync("TENANT_CREATED", It.IsAny<string>(), null, created.Id),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UnknownTenant_ReturnsFalse()
        {
            _tenants.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Tenant?)null);

            var result = await _service.UpdateAsync(Guid.NewGuid(), new Tenant { Name = "New Name" });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_OnlyUpdatesAllowedProfileFields()
        {
            var existing = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Old Name",
                Domain = "acme.com",
                ContactEmail = "old@acme.com",
                ContactPhone = "111",
                SubscriptionPlanId = Guid.NewGuid(),
                IsActive = true
            };
            _tenants.Setup(x => x.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
            _tenants.Setup(x => x.UpdateAsync(It.IsAny<Tenant>())).Returns(Task.CompletedTask);

            // The caller tries to change the domain and subscription plan through the payload.
            var result = await _service.UpdateAsync(existing.Id, new Tenant
            {
                Name = "New Name",
                Domain = "hacker.com",
                ContactEmail = "new@acme.com",
                ContactPhone = "222",
                SubscriptionPlanId = Guid.NewGuid(),
                IsActive = false
            });

            Assert.True(result);
            Assert.Equal("New Name", existing.Name);
            Assert.Equal("new@acme.com", existing.ContactEmail);
            Assert.Equal("222", existing.ContactPhone);
            Assert.False(existing.IsActive);
            // Domain and plan are never taken from the request payload.
            Assert.Equal("acme.com", existing.Domain);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesOnceThenReturnsFalse()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme" };
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);
            _tenants.Setup(x => x.UpdateAsync(It.IsAny<Tenant>())).Returns(Task.CompletedTask);

            var firstResult = await _service.DeleteAsync(tenant.Id);
            var secondResult = await _service.DeleteAsync(tenant.Id);

            Assert.True(firstResult);
            Assert.True(tenant.IsDeleted);
            Assert.False(secondResult);
            _logs.Verify(
                x => x.LogAsync("TENANT_DELETED", It.IsAny<string>(), null, tenant.Id),
                Times.Once);
        }

        [Fact]
        public async Task GetSettingsAsync_UnknownTenant_ReturnsNull()
        {
            _tenants.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Tenant?)null);

            var result = await _service.GetSettingsAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSettingsAsync_MissingName_Throws()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme" };
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateSettingsAsync(tenant.Id, new TenantSettingsDto { Name = "  " }));

            Assert.Equal("Workspace name is required.", ex.Message);
        }

        [Fact]
        public async Task UpdateSettingsAsync_ValidSettings_SavesAndAudits()
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Acme",
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            };
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);
            _tenants.Setup(x => x.UpdateAsync(It.IsAny<Tenant>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateSettingsAsync(tenant.Id, new TenantSettingsDto
            {
                Name = "  Acme Corp  ",
                ContactEmail = " hello@acme.com ",
                ContactPhone = "123",
                EmailNotifications = false,
                InAppNotifications = true
            });

            Assert.True(result);
            Assert.Equal("Acme Corp", tenant.Name);
            Assert.Equal("hello@acme.com", tenant.ContactEmail);
            Assert.False(tenant.EmailNotificationsEnabled);
            Assert.True(tenant.InAppNotificationsEnabled);
            _logs.Verify(
                x => x.LogAsync("TENANT_SETTINGS_UPDATED", It.IsAny<string>(), null, tenant.Id),
                Times.Once);
        }

        [Fact]
        public async Task GetTenantsPage_PassesParametersToRepository()
        {
            var paged = new SaaSPlatform.Application.DTOS.PagedResult<Tenant>
            {
                Data = new List<Tenant>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };
            _tenants.Setup(x => x.GetTenantsPage("Acme", "Pro", 1, 10)).ReturnsAsync(paged);

            var result = await _service.GetTenantsPage("Acme", "Pro", 1, 10);

            Assert.NotNull(result);
            _tenants.Verify(x => x.GetTenantsPage("Acme", "Pro", 1, 10), Times.Once);
        }
    }
}
