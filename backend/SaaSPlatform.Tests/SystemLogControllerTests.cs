using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class SystemLogControllerTests
    {
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly SystemLogController _controller;

        public SystemLogControllerTests()
        {
            _controller = new SystemLogController(_logs.Object);
        }

        [Fact]
        public async Task GetLogs_SuperAdmin_GetsGlobalLogsWithoutTenantFilter()
        {
            // SuperAdmin has no tenant claim: they see logs of every tenant.
            TestHelpers.SetUser(_controller, role: "SuperAdmin");
            _logs.Setup(x => x.GetLogsPage(null, null, null, null, null, 1, 20))
                .ReturnsAsync(new PagedResult<SystemLog>());

            var result = await _controller.GetLogs();

            Assert.IsType<OkObjectResult>(result);
            _logs.Verify(x => x.GetLogsPage(null, null, null, null, null, 1, 20), Times.Once);
        }

        [Fact]
        public async Task GetLogs_TenantUser_OnlyGetsOwnTenantLogs()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId, role: "Member");
            _logs.Setup(x => x.GetLogsPage(tenantId, null, null, null, null, 1, 20))
                .ReturnsAsync(new PagedResult<SystemLog>());

            var result = await _controller.GetLogs();

            Assert.IsType<OkObjectResult>(result);
            _logs.Verify(x => x.GetLogsPage(tenantId, null, null, null, null, 1, 20), Times.Once);
            _logs.Verify(x => x.GetLogsPage(null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetLogs_TenantUserWithoutTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller, role: "Member"); // no tenant claim

            var result = await _controller.GetLogs();

            Assert.IsType<UnauthorizedResult>(result);
            _logs.Verify(x => x.GetLogsPage(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetLogs_WithFilterParameters_PassesFiltersToRepository()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin");
            var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

            _logs.Setup(x => x.GetLogsPage(null, "LOGIN_FAILED", "test@domain.com", startDate, endDate, 2, 50))
                .ReturnsAsync(new PagedResult<SystemLog> { Page = 2, PageSize = 50, TotalCount = 10 });

            var result = await _controller.GetLogs("LOGIN_FAILED", "test@domain.com", startDate, endDate, 2, 50);

            Assert.IsType<OkObjectResult>(result);
            _logs.Verify(x => x.GetLogsPage(null, "LOGIN_FAILED", "test@domain.com", startDate, endDate, 2, 50), Times.Once);
        }

        [Fact]
        public async Task GetLogs_ClampsPageAndPageSize()
        {
            TestHelpers.SetUser(_controller, role: "SuperAdmin");

            _logs.Setup(x => x.GetLogsPage(null, null, null, null, null, 1, 200))
                .ReturnsAsync(new PagedResult<SystemLog>());

            // page = -5 should be clamped to 1, pageSize = 999 should be clamped to 200
            var result = await _controller.GetLogs(page: -5, pageSize: 999);

            Assert.IsType<OkObjectResult>(result);
            _logs.Verify(x => x.GetLogsPage(null, null, null, null, null, 1, 200), Times.Once);
        }
    }
}
