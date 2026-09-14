using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class BillingControllerTests
    {
        private readonly Mock<IBillingService> _billing = new();
        private readonly BillingController _controller;

        public BillingControllerTests()
        {
            _controller = new BillingController(_billing.Object);
        }

        [Fact]
        public async Task GetCurrentPlan_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller); // authenticated but no tenant claim

            var result = await _controller.GetCurrentPlan();

            Assert.IsType<UnauthorizedResult>(result);
            _billing.Verify(x => x.GetCurrentPlanAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentPlan_UnknownTenantPlan_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _billing.Setup(x => x.GetCurrentPlanAsync(It.IsAny<Guid>())).ReturnsAsync((CurrentPlanDto?)null);

            var result = await _controller.GetCurrentPlan();

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(TestHelpers.ReadMessage(notFound.Value));
        }

        [Fact]
        public async Task GetCurrentPlan_KnownPlan_ReturnsOkWithPlan()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _billing.Setup(x => x.GetCurrentPlanAsync(tenantId)).ReturnsAsync(new CurrentPlanDto
            {
                PlanName = "Basic",
                Price = 29.99m,
                BillingFrequency = "Monthly"
            });

            var result = await _controller.GetCurrentPlan();

            var ok = Assert.IsType<OkObjectResult>(result);
            var plan = Assert.IsType<CurrentPlanDto>(ok.Value);
            Assert.Equal("Basic", plan.PlanName);
        }

        [Fact]
        public async Task GetPayments_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetPayments();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetPayments_WithTenantClaim_OnlyLoadsThatTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _billing.Setup(x => x.GetPaymentHistoryAsync(tenantId))
                .ReturnsAsync(new List<PaymentHistoryItemDto>());

            var result = await _controller.GetPayments();

            Assert.IsType<OkObjectResult>(result);
            _billing.Verify(x => x.GetPaymentHistoryAsync(tenantId), Times.Once);
            _billing.Verify(x => x.GetPaymentHistoryAsync(It.Is<Guid>(id => id != tenantId)), Times.Never);
        }

        [Fact]
        public async Task GetPayment_PaymentFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            var paymentId = Guid.NewGuid();
            // The service only returns payments that belong to the caller's tenant.
            _billing.Setup(x => x.GetPaymentAsync(tenantId, paymentId)).ReturnsAsync((Payment?)null);

            var result = await _controller.GetPayment(paymentId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payment not found.", TestHelpers.ReadMessage(notFound.Value));
        }

        [Fact]
        public async Task GetPayment_OwnPayment_ReturnsOkWithPayment()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Amount = 29.99m,
                PaymentStatus = "Success"
            };
            _billing.Setup(x => x.GetPaymentAsync(tenantId, payment.Id)).ReturnsAsync(payment);

            var result = await _controller.GetPayment(payment.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Payment>(ok.Value);
            Assert.Equal(payment.Id, returned.Id);
        }
    }
}
