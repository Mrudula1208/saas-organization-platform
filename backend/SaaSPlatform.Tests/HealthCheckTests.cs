using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SaaSPlatform.API.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class HealthCheckTests
    {
        private readonly Mock<IDatabaseConnectionChecker> _checkerMock = new();

        [Fact]
        public async Task DatabaseHealthCheck_HealthyWhenDatabaseCanConnect()
        {
            _checkerMock.Setup(c => c.CanConnectAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var healthCheck = new DatabaseHealthCheck(_checkerMock.Object);
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("healthy", result.Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DatabaseHealthCheck_UnhealthyWhenDatabaseCannotConnect()
        {
            _checkerMock.Setup(c => c.CanConnectAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var healthCheck = new DatabaseHealthCheck(_checkerMock.Object);
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Cannot connect", result.Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DatabaseHealthCheck_UnhealthyWhenDatabaseThrows()
        {
            _checkerMock.Setup(c => c.CanConnectAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Connection timeout"));

            var healthCheck = new DatabaseHealthCheck(_checkerMock.Object);
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("failed", result.Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DatabaseHealthCheck_PassesCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            _checkerMock.Setup(c => c.CanConnectAsync(cts.Token))
                .ReturnsAsync(true);

            var healthCheck = new DatabaseHealthCheck(_checkerMock.Object);
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

            Assert.Equal(HealthStatus.Healthy, result.Status);
            _checkerMock.Verify(c => c.CanConnectAsync(cts.Token), Times.Once);
        }
    }
}
