using SaaSPlatform.Application.DTOS.Reports;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IReportService
    {
        Task<object> GetTenantDashboardAsync(Guid tenantId);
        Task<object> GetSuperAdminDashboardAsync();
        Task<TenantReportDto> GetTenantReportAsync(Guid tenantId);
        Task<AdminReportDto> GetAdminReportAsync();
        Task<ReportExportFileDto> ExportTenantReportPdfAsync(Guid tenantId);
        Task<ReportExportFileDto> ExportTenantReportExcelAsync(Guid tenantId);
        Task<ReportExportFileDto> ExportAdminReportPdfAsync();
        Task<ReportExportFileDto> ExportAdminReportExcelAsync();
    }
}
