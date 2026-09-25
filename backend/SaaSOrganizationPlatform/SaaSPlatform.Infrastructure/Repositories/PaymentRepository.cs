using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Application.DTOS.Payments;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class PaymentRepository:IPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync(Guid tenantId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<AdminTransactionDto>> GetAdminTransactionsAsync()
        {
            // Join payments with tenants and subscription plans for super admin ledger
            var query = from p in _context.Payments.AsNoTracking()
                        join t in _context.Tenants.AsNoTracking() on p.TenantId equals t.Id into tenantGroup
                        from t in tenantGroup.DefaultIfEmpty()
                        join plan in _context.SubscriptionPlans.AsNoTracking() on p.SubscriptionPlanId equals plan.Id into planGroup
                        from plan in planGroup.DefaultIfEmpty()
                        orderby p.PaymentDate descending, p.Id descending
                        select new AdminTransactionDto
                        {
                            Id = p.Id,
                            TenantName = t != null ? t.Name : "Unknown Organization",
                            Plan = plan != null ? plan.Name : "Standard",
                            Amount = p.Amount,
                            Date = p.PaymentDate,
                            Status = p.PaymentStatus,
                            InvoiceId = !string.IsNullOrEmpty(p.TransactionId) ? p.TransactionId : p.Id.ToString()
                        };

            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<PaymentHistoryItemDto>> GetPaymentHistoryAsync(Guid tenantId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .Select(p => new PaymentHistoryItemDto
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    TransactionId = p.TransactionId
                })
                .ToListAsync();
        }

        public async Task<BillingSummaryDto> GetPaymentSummaryAsync(Guid tenantId)
        {
            // One grouped SQL query returns all summary values; no payment
            // entities are materialised just to count, sum, or find a date.
            var summary = await _context.Payments
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
                .GroupBy(_ => 1)
                .Select(g => new BillingSummaryDto
                {
                    TotalPaid = g
                        .Where(p => p.PaymentStatus.ToLower() == "success")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    TotalPayments = g.Count(),
                    SuccessfulPayments = g.Count(p => p.PaymentStatus.ToLower() == "success"),
                    FailedPayments = g.Count(p => p.PaymentStatus.ToLower() != "success"),
                    LastPaymentDate = g
                        .Where(p => p.PaymentStatus.ToLower() == "success")
                        .Max(p => (DateTime?)p.PaymentDate)
                })
                .SingleOrDefaultAsync();

            return summary ?? new BillingSummaryDto();
        }

        public async Task<DateTime?> GetLastSuccessfulPaymentDateAsync(Guid tenantId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.PaymentStatus.ToLower() == "success")
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .Select(p => (DateTime?)p.PaymentDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Payment> GetByIdAsync(Guid Id)
        {
            return await _context.Payments.FindAsync(Id);
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeleteAsync(Payment payment)
        {
            _context.Remove(payment);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}
