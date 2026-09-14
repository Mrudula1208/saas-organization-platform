using Moq;
using SaaSPlatform.Application.DTOS.Reports;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using System.Text;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ReportServiceTests
    {
        private readonly Mock<IReportRepository> _reports = new();
        private readonly ReportService _service;

        public ReportServiceTests()
        {
            _service = new ReportService(_reports.Object);
        }

        private void SetupTenantData(Guid tenantId, string tenantName)
        {
            _reports.Setup(x => x.GetTenantReportDataAsync(tenantId)).ReturnsAsync(new TenantReportDto
            {
                TotalProjects = 3,
                TotalTasks = 10,
                CompletedTasks = 4,
                PendingTasks = 4,
                InProgressTasks = 2,
                TotalMembers = 5,
                AvgTasksPerMember = 2,
                CompletionRate = 40
            });
            _reports.Setup(x => x.GetTenantNameAsync(tenantId)).ReturnsAsync(tenantName);
            _reports.Setup(x => x.GetTenantProjectBreakdownAsync(tenantId))
                .ReturnsAsync(new List<TenantProjectBreakdownDto>
                {
                    new() { Name = "Website", Status = "Active", TaskCount = 6, CompletedTaskCount = 4 },
                    new() { Name = "Mobile App", Status = "Pending", TaskCount = 4, CompletedTaskCount = 0 }
                });
        }

        [Fact]
        public async Task GetTenantReportAsync_PassesTenantIdToRepository()
        {
            var tenantId = Guid.NewGuid();
            var expected = new TenantReportDto { TotalProjects = 2 };
            _reports.Setup(x => x.GetTenantReportDataAsync(tenantId)).ReturnsAsync(expected);

            var result = await _service.GetTenantReportAsync(tenantId);

            Assert.Same(expected, result);
            // The report must only ever be built from the caller's tenant.
            _reports.Verify(x => x.GetTenantReportDataAsync(tenantId), Times.Once);
            _reports.Verify(x => x.GetTenantReportDataAsync(It.Is<Guid>(id => id != tenantId)), Times.Never);
        }

        [Fact]
        public async Task ExportTenantReportPdfAsync_GeneratesPdfFile()
        {
            var tenantId = Guid.NewGuid();
            SetupTenantData(tenantId, "Acme");

            var file = await _service.ExportTenantReportPdfAsync(tenantId);

            Assert.NotEmpty(file.Content);
            Assert.Equal("application/pdf", file.ContentType);
            Assert.StartsWith("workspace-analytics_", file.FileName);
            Assert.EndsWith(".pdf", file.FileName);
            // A real PDF document starts with the %PDF header.
            Assert.Equal("%PDF", Encoding.ASCII.GetString(file.Content, 0, 4));
        }

        [Fact]
        public async Task ExportTenantReportExcelAsync_GeneratesXlsxFile()
        {
            var tenantId = Guid.NewGuid();
            SetupTenantData(tenantId, "Acme");

            var file = await _service.ExportTenantReportExcelAsync(tenantId);

            Assert.NotEmpty(file.Content);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
            Assert.EndsWith(".xlsx", file.FileName);
            // xlsx files are zip archives, which start with the "PK" magic bytes.
            Assert.Equal((byte)'P', file.Content[0]);
            Assert.Equal((byte)'K', file.Content[1]);
        }

        [Fact]
        public async Task Export_PdfOnlyUsesCallerTenantData()
        {
            var tenantId = Guid.NewGuid();
            SetupTenantData(tenantId, "Acme");

            await _service.ExportTenantReportPdfAsync(tenantId);

            _reports.Verify(x => x.GetTenantReportDataAsync(tenantId), Times.Once);
            _reports.Verify(x => x.GetTenantNameAsync(tenantId), Times.Once);
            _reports.Verify(x => x.GetTenantProjectBreakdownAsync(tenantId), Times.Once);
        }

        [Fact]
        public async Task Export_UnknownTenantName_FallsBackToWorkspace()
        {
            var tenantId = Guid.NewGuid();
            SetupTenantData(tenantId, "   ");

            var file = await _service.ExportTenantReportExcelAsync(tenantId);

            // A blank tenant name must not produce an unusable file name.
            Assert.StartsWith("workspace-analytics_Workspace", file.FileName);
        }

        [Fact]
        public async Task Export_UnsafeTenantName_IsSanitizedInFileName()
        {
            var tenantId = Guid.NewGuid();
            SetupTenantData(tenantId, "Acme / <Corp>");

            var file = await _service.ExportTenantReportExcelAsync(tenantId);

            Assert.DoesNotContain("/", file.FileName);
            Assert.DoesNotContain("<", file.FileName);
            Assert.DoesNotContain(">", file.FileName);
        }
    }
}
