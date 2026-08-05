using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IReportRepository
    {
        Task<int> GetUserCountAsync(Guid tenantId);
        Task<int> GetPaymentCountAsync(Guid tennantId);
        Task<decimal> GetTotalRevenueAsync(Guid tenantId);
        Task<object> GetTenantDashboardDataAsync(Guid tenantId);
        Task<object> GetSuperAdminDashboardDataAsync();
    }
}
