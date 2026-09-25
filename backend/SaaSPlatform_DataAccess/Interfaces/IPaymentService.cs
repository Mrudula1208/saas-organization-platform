using SaaSPlatform.Application.DTOS.Payments;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllAsync(Guid tenantId);
        Task<IReadOnlyList<AdminTransactionDto>> GetAdminTransactionsAsync();
        Task<Payment> GetByIdAsync(Guid Id);
        Task<Payment>CreateAsync(Payment payment, Guid? userId = null);
        Task<bool>DeleteAsync(Guid Id, Guid? userId = null);
    }
}
