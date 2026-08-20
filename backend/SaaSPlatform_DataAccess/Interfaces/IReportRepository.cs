using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IReportRepository
    {
        Task<int> GetUserCountAsync(Guid tenantId);
        Task<int> GetPaymentCountAsync(Guid tenantId);
        Task<decimal> GetTotalRevenueAsync(Guid tenantId);
        Task<object> GetTenantDashboardDataAsync(Guid tenantId);
        Task<object> GetSuperAdminDashboardDataAsync();
        Task<object> GetTenantReportDataAsync(Guid tenantId);
        Task<object> GetAdminReportDataAsync();
    }
}
