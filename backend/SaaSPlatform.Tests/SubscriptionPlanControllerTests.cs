using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS.SubscriptionPlans;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class SubscriptionPlanControllerTests
    {
        private readonly Mock<ISubscriptionPlanService> _plans = new();
        private readonly SubscriptionPlanController _controller;

        public SubscriptionPlanControllerTests()
        {
            _controller = new SubscriptionPlanController(_plans.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithPlans()
        {
            // Listing plans is public: the registration page needs it.
            _plans.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<SubscriptionPlan> { new() { Id = Guid.NewGuid(), Name = "Basic" } });

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetById_UnknownPlan_ReturnsNotFound()
        {
            _plans.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_KnownPlan_ReturnsOk()
        {
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Basic", Price = 29.99m };
            _plans.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

            var result = await _controller.GetById(plan.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SubscriptionPlan>(ok.Value);
            Assert.Equal("Basic", returned.Name);
        }

        [Fact]
        public async Task Create_ValidPlan_ReturnsOkWithCreatedPlan()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin", userId: Guid.NewGuid());
            _plans.Setup(x => x.AddAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<Guid?>()))
                .ReturnsAsync((SubscriptionPlan p, Guid? _) => p);

            var result = await _controller.Create(new CreateSubscriptionPlanDto
            {
                Name = "Pro",
                Price = 99.99m,
                MaxUsers = 50,
                MaxProjects = 25,
                StorageLimitMB = 5120
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var created = Assert.IsType<SubscriptionPlan>(ok.Value);
            Assert.Equal("Pro", created.Name);
            Assert.True(created.IsActive);
        }

        [Fact]
        public async Task Update_UnknownPlan_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin", userId: Guid.NewGuid());
            _plans.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<SubscriptionPlan>(), It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            var result = await _controller.Update(Guid.NewGuid(), new UpdateSubscriptionPlanDto { Name = "Pro" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_KnownPlan_ReturnsNoContent()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin", userId: Guid.NewGuid());
            _plans.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<SubscriptionPlan>(), It.IsAny<Guid?>()))
                .ReturnsAsync(true);

            var result = await _controller.Update(Guid.NewGuid(), new UpdateSubscriptionPlanDto { Name = "Pro" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_UnknownPlan_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin", userId: Guid.NewGuid());
            _plans.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid?>())).ReturnsAsync(false);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
