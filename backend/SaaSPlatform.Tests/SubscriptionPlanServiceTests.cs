using Moq;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class SubscriptionPlanServiceTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _plans = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly SubscriptionPlanService _service;

        public SubscriptionPlanServiceTests()
        {
            _service = new SubscriptionPlanService(_plans.Object, _logs.Object);
        }

        private static SubscriptionPlan CreatePlan(string name = "Basic")
        {
            return new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = name,
                Price = 29.99m,
                MaxUsers = 10,
                MaxProjects = 5,
                StorageLimitMB = 1024,
                IsActive = true
            };
        }

        [Fact]
        public async Task AddAsync_MissingName_Throws()
        {
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.AddAsync(new SubscriptionPlan { Name = "" }));

            Assert.Equal("Subscription Plan is required", ex.Message);
            _plans.Verify(x => x.AddAsync(It.IsAny<SubscriptionPlan>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_ValidPlan_CreatesAndAuditsWithGlobalScope()
        {
            var plan = CreatePlan();
            _plans.Setup(x => x.AddAsync(It.IsAny<SubscriptionPlan>()))
                .ReturnsAsync((SubscriptionPlan p) => p);

            var created = await _service.AddAsync(plan, Guid.NewGuid());

            Assert.Equal(plan.Id, created.Id);
            // Subscription plans are platform level: the audit entry is not owned by a tenant.
            _logs.Verify(
                x => x.LogAsync(
                    "SUBSCRIPTION_PLAN_CREATED",
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.Is<Guid?>(tenantId => tenantId == null)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UnknownPlan_ReturnsFalse()
        {
            _plans.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

            var result = await _service.UpdateAsync(Guid.NewGuid(), CreatePlan());

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ExistingPlan_UpdatesLimitsAndAudits()
        {
            var existing = CreatePlan();
            _plans.Setup(x => x.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
            _plans.Setup(x => x.UpdateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync(true);

            var incoming = CreatePlan("Pro");
            incoming.Price = 99.99m;
            incoming.MaxUsers = 50;
            incoming.MaxProjects = 25;
            incoming.StorageLimitMB = 5120;
            incoming.IsActive = false;

            var result = await _service.UpdateAsync(existing.Id, incoming, Guid.NewGuid());

            Assert.True(result);
            Assert.Equal("Pro", existing.Name);
            Assert.Equal(99.99m, existing.Price);
            Assert.Equal(50, existing.MaxUsers);
            Assert.Equal(25, existing.MaxProjects);
            Assert.Equal(5120, existing.StorageLimitMB);
            Assert.False(existing.IsActive);
            _logs.Verify(
                x => x.LogAsync("SUBSCRIPTION_PLAN_UPDATED", It.IsAny<string>(), It.IsAny<Guid?>(), null),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UnknownPlan_ReturnsFalse()
        {
            _plans.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

            var result = await _service.DeleteAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingPlan_DeletesAndAudits()
        {
            var plan = CreatePlan();
            _plans.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
            _plans.Setup(x => x.DeleteAsync(plan)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(plan.Id, Guid.NewGuid());

            Assert.True(result);
            _plans.Verify(x => x.DeleteAsync(plan), Times.Once);
            _logs.Verify(
                x => x.LogAsync("SUBSCRIPTION_PLAN_DELETED", It.IsAny<string>(), It.IsAny<Guid?>(), null),
                Times.Once);
        }
    }
}
