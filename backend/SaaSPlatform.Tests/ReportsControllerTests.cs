using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS.Reports;
using SaaSPlatform.Application.Interfaces;
using System.Text;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ReportsControllerTests
    {
        private readonly Mock<IReportService> _reports = new();
        private readonly ReportsController _controller;

        public ReportsControllerTests()
        {
            _controller = new ReportsController(_reports.Object);
        }

        [Fact]
        public async Task GetDashboard_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetDashboard();

            Assert.IsType<UnauthorizedResult>(result);
            _reports.Verify(x => x.GetTenantDashboardAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetDashboard_WithTenantClaim_UsesClaimTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _reports.Setup(x => x.GetTenantDashboardAsync(tenantId)).ReturnsAsync(new { totalProjects = 3 });

            var result = await _controller.GetDashboard();

            Assert.IsType<OkObjectResult>(result);
            _reports.Verify(x => x.GetTenantDashboardAsync(tenantId), Times.Once);
        }

        [Fact]
        public async Task ExportPdf_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.ExportPdf();

            Assert.IsType<UnauthorizedResult>(result);
            _reports.Verify(x => x.ExportTenantReportPdfAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ExportPdf_Success_ReturnsFileWithDownloadName()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _reports.Setup(x => x.ExportTenantReportPdfAsync(tenantId)).ReturnsAsync(new ReportExportFileDto
            {
                Content = Encoding.ASCII.GetBytes("%PDF-1.7 test"),
                ContentType = "application/pdf",
                FileName = "workspace-analytics_Acme_20260923.pdf"
            });

            var result = await _controller.ExportPdf();

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", file.ContentType);
            Assert.Equal("workspace-analytics_Acme_20260923.pdf", file.FileDownloadName);
        }

        [Fact]
        public async Task ExportPdf_ServiceThrows_Returns500WithMessage()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _reports.Setup(x => x.ExportTenantReportPdfAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("QuestPDF crashed"));

            var result = await _controller.ExportPdf();

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var message = TestHelpers.ReadMessage(objectResult.Value);
            Assert.Contains("Failed to generate PDF report", message);
        }

        [Fact]
        public async Task ExportExcel_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.ExportExcel();

            Assert.IsType<UnauthorizedResult>(result);
            _reports.Verify(x => x.ExportTenantReportExcelAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ExportExcel_ServiceThrows_Returns500WithMessage()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _reports.Setup(x => x.ExportTenantReportExcelAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("EPPlus crashed"));

            var result = await _controller.ExportExcel();

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var message = TestHelpers.ReadMessage(objectResult.Value);
            Assert.Contains("Failed to generate Excel report", message);
        }
    }
}
