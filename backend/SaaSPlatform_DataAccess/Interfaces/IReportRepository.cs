using SaaSPlatform.Application.DTOS.Reports;
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
        Task<TenantReportDto> GetTenantReportDataAsync(Guid tenantId);
        Task<AdminReportDto> GetAdminReportDataAsync();
        Task<string> GetTenantNameAsync(Guid tenantId);
        Task<System.Collections.Generic.List<TenantProjectBreakdownDto>> GetTenantProjectBreakdownAsync(Guid tenantId);
    }
}
