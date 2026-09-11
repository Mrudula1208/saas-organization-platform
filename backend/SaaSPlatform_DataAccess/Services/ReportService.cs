using SaaSPlatform.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<object> GetTenantDashboardAsync(Guid tenantId)
        {
            return await _reportRepository.GetTenantDashboardDataAsync(tenantId);
        }

        public async Task<object> GetSuperAdminDashboardAsync()
        {
            return await _reportRepository.GetSuperAdminDashboardDataAsync();
        }

        public async Task<object> GetTenantReportAsync(Guid tenantId)
        {
            return await _reportRepository.GetTenantReportDataAsync(tenantId);
        }

        public async Task<object> GetAdminReportAsync()
        {
            return await _reportRepository.GetAdminReportDataAsync();
        }
    }
}
