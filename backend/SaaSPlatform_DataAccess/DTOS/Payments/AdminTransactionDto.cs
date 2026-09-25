using System;

namespace SaaSPlatform.Application.DTOS.Payments
{
    public class AdminTransactionDto
    {
        public Guid Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
    }
}
