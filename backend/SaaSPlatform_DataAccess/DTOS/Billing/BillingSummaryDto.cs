using System;

namespace SaaSPlatform.Application.DTOS.Billing
{
    public class BillingSummaryDto
    {
        public decimal TotalPaid { get; set; }
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public CurrentPlanDto? CurrentPlan { get; set; }
    }
}