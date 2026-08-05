using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IReportService
    {
        Task<object> GetTenantDashboardAsync(Guid tenantId);
        Task<object> GetSuperAdminDashboardAsync();
    }
}
