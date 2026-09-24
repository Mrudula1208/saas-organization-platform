using Microsoft.Extensions.Diagnostics.HealthChecks;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaaSPlatform.API.HealthChecks
{
    public interface IDatabaseConnectionChecker
    {
        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    }

    public class EfCoreDatabaseConnectionChecker : IDatabaseConnectionChecker
    {
        private readonly ApplicationDbContext _context;

        public EfCoreDatabaseConnectionChecker(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            return _context.Database.CanConnectAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Probes database connectivity for production health monitoring, container orchestrators, and load balancers.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IDatabaseConnectionChecker _checker;

        public DatabaseHealthCheck(ApplicationDbContext context)
            : this(new EfCoreDatabaseConnectionChecker(context))
        {
        }

        public DatabaseHealthCheck(IDatabaseConnectionChecker checker)
        {
            _checker = checker;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _checker.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database connection is healthy.");
                }

                return HealthCheckResult.Unhealthy("Cannot connect to SQL Server database.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}", ex);
            }
        }
    }
}
