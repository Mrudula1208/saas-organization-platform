using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IBillingService
    {
        Task<CurrentPlanDto?> GetCurrentPlanAsync(Guid tenantId);
        Task<IEnumerable<PaymentHistoryItemDto>> GetPaymentHistoryAsync(Guid tenantId);
        Task<Payment?> GetPaymentAsync(Guid tenantId, Guid paymentId);
        Task<BillingSummaryDto> GetBillingSummaryAsync(Guid tenantId);
    }
}