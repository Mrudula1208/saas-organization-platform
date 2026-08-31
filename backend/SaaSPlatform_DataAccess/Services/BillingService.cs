using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class BillingService : IBillingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BillingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CurrentPlanDto?> GetCurrentPlanAsync(Guid tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return null;
            }

            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(tenant.SubscriptionPlanId);
            if (plan == null)
            {
                return null;
            }

            var lastPaymentDate = await GetLastSuccessfulPaymentDateAsync(tenantId);
            var nextBillingDate = lastPaymentDate.HasValue
                ? lastPaymentDate.Value.AddMonths(1)
                : tenant.CreatedAt.AddMonths(1);

            return new CurrentPlanDto
            {
                SubscriptionPlanId = plan.Id,
                PlanName = plan.Name,
                Price = plan.Price,
                MaxUsers = plan.MaxUsers,
                MaxProjects = plan.MaxProjects,
                StorageLimitMB = plan.StorageLimitMB,
                BillingFrequency = "Monthly",
                NextBillingDate = nextBillingDate
            };
        }

        public async Task<IEnumerable<PaymentHistoryItemDto>> GetPaymentHistoryAsync(Guid tenantId)
        {
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);

            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentHistoryItemDto
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    TransactionId = p.TransactionId
                })
                .ToList();
        }

        public async Task<Payment?> GetPaymentAsync(Guid tenantId, Guid paymentId)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null || payment.TenantId != tenantId)
            {
                return null;
            }
            return payment;
        }

        public async Task<BillingSummaryDto> GetBillingSummaryAsync(Guid tenantId)
        {
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);
            var list = payments.ToList();

            var successful = list.Where(p => IsSuccessful(p.PaymentStatus)).ToList();

            return new BillingSummaryDto
            {
                TotalPaid = successful.Sum(p => p.Amount),
                TotalPayments = list.Count,
                SuccessfulPayments = successful.Count,
                FailedPayments = list.Count - successful.Count,
                LastPaymentDate = successful.OrderByDescending(p => p.PaymentDate)
                    .Select(p => (DateTime?)p.PaymentDate)
                    .FirstOrDefault(),
                CurrentPlan = await GetCurrentPlanAsync(tenantId)
            };
        }

        private async Task<DateTime?> GetLastSuccessfulPaymentDateAsync(Guid tenantId)
        {
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);
            return payments
                .Where(p => IsSuccessful(p.PaymentStatus))
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => (DateTime?)p.PaymentDate)
                .FirstOrDefault();
        }

        private static bool IsSuccessful(string status)
        {
            return string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase);
        }
    }
}