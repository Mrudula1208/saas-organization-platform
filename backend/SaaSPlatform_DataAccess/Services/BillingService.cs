using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model.Entities;
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
            // Check the tenant before doing any payment lookup. This keeps
            // unknown-tenant requests cheap and preserves the old behaviour.
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return null;
            }

            var lastPaymentDate = await GetLastSuccessfulPaymentDateAsync(tenantId);
            return await BuildCurrentPlanAsync(tenant, lastPaymentDate);
        }

        private async Task<CurrentPlanDto?> GetCurrentPlanAsync(Guid tenantId, DateTime? lastPaymentDate)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null || tenant.IsDeleted)
            {
                return null;
            }

            return await BuildCurrentPlanAsync(tenant, lastPaymentDate);
        }

        private async Task<CurrentPlanDto?> BuildCurrentPlanAsync(Tenant tenant, DateTime? lastPaymentDate)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(tenant.SubscriptionPlanId);
            if (plan == null)
            {
                return null;
            }

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
            var history = await _unitOfWork.Payments.GetPaymentHistoryAsync(tenantId);
            if (history != null)
            {
                return history;
            }

            // Compatibility path for older repository implementations.
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);
            if (payments == null)
            {
                return Enumerable.Empty<PaymentHistoryItemDto>();
            }

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
            var summary = await _unitOfWork.Payments.GetPaymentSummaryAsync(tenantId);
            if (summary != null)
            {
                summary.CurrentPlan = await GetCurrentPlanAsync(tenantId, summary.LastPaymentDate);
                return summary;
            }

            // Compatibility path for older repository implementations.
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);
            var list = payments?.ToList() ?? new List<Payment>();
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
            var lastPaymentDate = await _unitOfWork.Payments.GetLastSuccessfulPaymentDateAsync(tenantId);
            if (lastPaymentDate.HasValue)
            {
                return lastPaymentDate;
            }

            // Keep the old fallback for compatibility with repositories that
            // predate the SQL projection. In the normal path a missing value means
            // that the tenant has no successful payment.
            var payments = await _unitOfWork.Payments.GetAllAsync(tenantId);
            if (payments == null)
            {
                return null;
            }

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