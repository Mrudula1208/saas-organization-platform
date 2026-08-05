using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync(Guid tenantId);
        Task<Payment> GetByIdAsync(Guid Id);
        Task<Payment>AddAsync(Payment payment);
        Task<bool> DeleteAsync(Payment payment);
    }
}
