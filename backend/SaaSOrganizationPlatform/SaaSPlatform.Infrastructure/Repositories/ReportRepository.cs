using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class ReportRepository:IReportRepository
    {
        private readonly ApplicationDbContext _context;
        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task <int>GetUserCountAsync(Guid tenantId)
        {
            return await _context.Users.CountAsync(u => u.TenantId == tenantId);
        }

        public async Task<int>GetPaymentCountAsync(Guid tenantId)
        {
            return await _context.Payments.CountAsync(p => p.TenantId == tenantId);
        }

        public async Task<decimal>GetTotalRevenueAsync(Guid tenantId)
        {
            return await _context.Payments
                .Where(p => p.TenantId == tenantId)
                .SumAsync(p => p.Amount);
        }
    }
}
