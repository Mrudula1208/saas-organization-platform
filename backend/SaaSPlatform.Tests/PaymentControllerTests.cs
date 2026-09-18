using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class PaymentControllerTests
    {
        private readonly Mock<IPaymentService> _payments = new();
        private readonly PaymentController _controller;

        public PaymentControllerTests()
        {
            _controller = new PaymentController(_payments.Object);
        }

        [Fact]
        public async Task GetAll_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetAll();

            Assert.IsType<UnauthorizedResult>(result);
            _payments.Verify(x => x.GetAllAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAll_WithTenantClaim_OnlyCallsServiceForThatTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _payments.Setup(x => x.GetAllAsync(tenantId)).ReturnsAsync(new List<Payment>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<IEnumerable<Payment>>(ok.Value);
            _payments.Verify(x => x.GetAllAsync(tenantId), Times.Once);
        }

        [Fact]
        public async Task Delete_PaymentFromAnotherTenant_ReturnsNotFound()
        {
            var callerTenantId = Guid.NewGuid();
            var foreignTenantId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            TestHelpers.SetUser(_controller, tenantId: callerTenantId, role: "TenantAdmin");
            _payments.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(new Payment { Id = paymentId, TenantId = foreignTenantId });

            var result = await _controller.Delete(paymentId);

            // Must return 404 to avoid leaking existence of another tenant's payment record
            Assert.IsType<NotFoundResult>(result);
            _payments.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
        }

        [Fact]
        public async Task Delete_OwnTenantPayment_DeletesAndReturnsNoContent()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            TestHelpers.SetUser(_controller, tenantId: tenantId, role: "TenantAdmin", userId: userId);
            _payments.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(new Payment { Id = paymentId, TenantId = tenantId });
            _payments.Setup(x => x.DeleteAsync(paymentId, userId)).ReturnsAsync(true);

            var result = await _controller.Delete(paymentId);

            Assert.IsType<NoContentResult>(result);
            _payments.Verify(x => x.DeleteAsync(paymentId, userId), Times.Once);
        }

        [Fact]
        public async Task Delete_AsSuperAdmin_CanDeleteCrossTenantPayment()
        {
            var callerTenantId = Guid.NewGuid();
            var foreignTenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            TestHelpers.SetUser(_controller, tenantId: callerTenantId, role: "SuperAdmin", userId: userId);
            _payments.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(new Payment { Id = paymentId, TenantId = foreignTenantId });
            _payments.Setup(x => x.DeleteAsync(paymentId, userId)).ReturnsAsync(true);

            var result = await _controller.Delete(paymentId);

            Assert.IsType<NoContentResult>(result);
            _payments.Verify(x => x.DeleteAsync(paymentId, userId), Times.Once);
        }
    }
}
