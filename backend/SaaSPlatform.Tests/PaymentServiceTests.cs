using Moq;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _payments = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            _service = new PaymentService(_payments.Object, _logs.Object);
        }

        private static Payment CreateValidPayment(Guid tenantId)
        {
            return new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                Amount = 150m,
                PaymentMethod = "Card"
            };
        }

        [Fact]
        public async Task CreateAsync_NegativeAmount_Throws()
        {
            var payment = CreateValidPayment(Guid.NewGuid());
            payment.Amount = -1;

            await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(payment));

            _payments.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidPayment_SetsStatusAndTransactionAndAudits()
        {
            var tenantId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var payment = CreateValidPayment(tenantId);
            _payments.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync((Payment p) => p);

            var created = await _service.CreateAsync(payment, actorId);

            Assert.Equal("Success", created.PaymentStatus);
            Assert.False(string.IsNullOrWhiteSpace(created.TransactionId));
            Assert.True(created.PaymentDate <= DateTime.UtcNow);
            _logs.Verify(
                x => x.LogAsync(
                    "PAYMENT_RECEIVED",
                    It.Is<string>(m => m.Contains("150") && m.Contains("Card")),
                    actorId,
                    tenantId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UnknownPayment_ReturnsFalse()
        {
            _payments.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

            var result = await _service.DeleteAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingPayment_DeletesAndAudits()
        {
            var payment = CreateValidPayment(Guid.NewGuid());
            payment.PaymentStatus = "Success";
            payment.TransactionId = "tx-123";
            _payments.Setup(x => x.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _payments.Setup(x => x.DeleteAsync(payment)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(payment.Id, Guid.NewGuid());

            Assert.True(result);
            _payments.Verify(x => x.DeleteAsync(payment), Times.Once);
            _logs.Verify(
                x => x.LogAsync("PAYMENT_DELETED", It.IsAny<string>(), It.IsAny<Guid?>(), payment.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task GetAdminTransactionsAsync_ReturnsTransactionsList()
        {
            var expected = new List<SaaSPlatform.Application.DTOS.Payments.AdminTransactionDto>
            {
                new SaaSPlatform.Application.DTOS.Payments.AdminTransactionDto
                {
                    Id = Guid.NewGuid(),
                    TenantName = "Acme Corp",
                    Plan = "Pro",
                    Amount = 45m,
                    Status = "Success"
                }
            };
            _payments.Setup(x => x.GetAdminTransactionsAsync()).ReturnsAsync(expected);

            var result = await _service.GetAdminTransactionsAsync();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Acme Corp", result[0].TenantName);
        }
    }
}
