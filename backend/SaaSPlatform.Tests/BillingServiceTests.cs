using Moq;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class BillingServiceTests
    {
        private readonly Mock<ITenantRepository> _tenants = new();
        private readonly Mock<ISubscriptionPlanRepository> _plans = new();
        private readonly Mock<IPaymentRepository> _payments = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly BillingService _service;

        public BillingServiceTests()
        {
            _unitOfWork.Setup(x => x.Tenants).Returns(_tenants.Object);
            _unitOfWork.Setup(x => x.SubscriptionPlans).Returns(_plans.Object);
            _unitOfWork.Setup(x => x.Payments).Returns(_payments.Object);

            _service = new BillingService(_unitOfWork.Object);
        }

        private static Tenant CreateTenant(DateTime createdAt)
        {
            return new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Acme",
                Domain = "acme.com",
                SubscriptionPlanId = Guid.NewGuid(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = createdAt
            };
        }

        private static Payment CreatePayment(Guid tenantId, decimal amount, string status, DateTime date)
        {
            return new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                Amount = amount,
                PaymentMethod = "Card",
                PaymentStatus = status,
                TransactionId = Guid.NewGuid().ToString(),
                PaymentDate = date
            };
        }

        [Fact]
        public async Task GetCurrentPlanAsync_UnknownTenant_ReturnsNull()
        {
            _tenants.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Tenant?)null);

            var result = await _service.GetCurrentPlanAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentPlanAsync_DeletedTenant_ReturnsNull()
        {
            var tenant = CreateTenant(DateTime.UtcNow);
            tenant.IsDeleted = true;
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);

            var result = await _service.GetCurrentPlanAsync(tenant.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentPlanAsync_NoPayments_BillsOneMonthAfterTenantCreated()
        {
            var createdAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var tenant = CreateTenant(createdAt);
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);
            _plans.Setup(x => x.GetByIdAsync(tenant.SubscriptionPlanId))
                .ReturnsAsync(new SubscriptionPlan
                {
                    Id = tenant.SubscriptionPlanId,
                    Name = "Basic",
                    Price = 29.99m,
                    MaxUsers = 10,
                    MaxProjects = 5,
                    StorageLimitMB = 1024,
                    IsActive = true
                });
            _payments.Setup(x => x.GetAllAsync(tenant.Id)).ReturnsAsync(new List<Payment>());

            var plan = await _service.GetCurrentPlanAsync(tenant.Id);

            Assert.NotNull(plan);
            Assert.Equal("Basic", plan!.PlanName);
            Assert.Equal("Monthly", plan.BillingFrequency);
            Assert.Equal(createdAt.AddMonths(1), plan.NextBillingDate);
        }

        [Fact]
        public async Task GetCurrentPlanAsync_UsesLastSuccessfulPaymentForNextBillingDate()
        {
            var tenant = CreateTenant(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _tenants.Setup(x => x.GetByIdAsync(tenant.Id)).ReturnsAsync(tenant);
            _plans.Setup(x => x.GetByIdAsync(tenant.SubscriptionPlanId))
                .ReturnsAsync(new SubscriptionPlan { Id = tenant.SubscriptionPlanId, Name = "Basic", Price = 10m });
            _payments.Setup(x => x.GetAllAsync(tenant.Id)).ReturnsAsync(new List<Payment>
            {
                CreatePayment(tenant.Id, 10m, "Success", new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenant.Id, 10m, "Success", new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenant.Id, 10m, "Failed", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))
            });

            var plan = await _service.GetCurrentPlanAsync(tenant.Id);

            // The failed payment must not move the billing date; the last success is 2026-07-10.
            Assert.Equal(new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc), plan!.NextBillingDate);
        }

        [Fact]
        public async Task GetPaymentAsync_PaymentFromAnotherTenant_ReturnsNull()
        {
            var payment = CreatePayment(Guid.NewGuid(), 10m, "Success", DateTime.UtcNow);
            _payments.Setup(x => x.GetByIdAsync(payment.Id)).ReturnsAsync(payment);

            // Another tenant asks for this payment id.
            var result = await _service.GetPaymentAsync(Guid.NewGuid(), payment.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPaymentAsync_OwnPayment_ReturnsPayment()
        {
            var tenantId = Guid.NewGuid();
            var payment = CreatePayment(tenantId, 10m, "Success", DateTime.UtcNow);
            _payments.Setup(x => x.GetByIdAsync(payment.Id)).ReturnsAsync(payment);

            var result = await _service.GetPaymentAsync(tenantId, payment.Id);

            Assert.NotNull(result);
            Assert.Equal(payment.Id, result!.Id);
        }

        [Fact]
        public async Task GetBillingSummaryAsync_CountsOnlySuccessfulPayments()
        {
            var tenantId = Guid.NewGuid();
            var tenant = CreateTenant(DateTime.UtcNow);
            tenant.Id = tenantId;
            _tenants.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);
            _plans.Setup(x => x.GetByIdAsync(tenant.SubscriptionPlanId))
                .ReturnsAsync(new SubscriptionPlan { Id = tenant.SubscriptionPlanId, Name = "Basic", Price = 10m });
            _payments.Setup(x => x.GetAllAsync(tenantId)).ReturnsAsync(new List<Payment>
            {
                CreatePayment(tenantId, 100m, "Success", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenantId, 50m, "Success", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenantId, 999m, "Failed", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))
            });

            var summary = await _service.GetBillingSummaryAsync(tenantId);

            Assert.Equal(150m, summary.TotalPaid); // the failed 999 is not counted
            Assert.Equal(3, summary.TotalPayments);
            Assert.Equal(2, summary.SuccessfulPayments);
            Assert.Equal(1, summary.FailedPayments);
            Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), summary.LastPaymentDate);
            Assert.NotNull(summary.CurrentPlan);
        }

        [Fact]
        public async Task GetPaymentHistoryAsync_OrdersNewestFirst()
        {
            var tenantId = Guid.NewGuid();
            _payments.Setup(x => x.GetAllAsync(tenantId)).ReturnsAsync(new List<Payment>
            {
                CreatePayment(tenantId, 10m, "Success", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenantId, 20m, "Success", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreatePayment(tenantId, 30m, "Success", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc))
            });

            var history = (await _service.GetPaymentHistoryAsync(tenantId)).ToList();

            Assert.Equal(3, history.Count);
            Assert.Equal(20m, history[0].Amount); // March payment first
            Assert.Equal(30m, history[1].Amount);
            Assert.Equal(10m, history[2].Amount);
        }
    }
}
