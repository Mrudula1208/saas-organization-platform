using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ITenantRepository Tenants { get; }
        IProjectRepository Projects { get; }
        ITaskRepository Tasks { get; }
        ISubscriptionPlanRepository SubscriptionPlans { get; }
        IPaymentRepository Payments { get; }
        IReportRepository Reports { get; }
        IProjectMemberRepository ProjectMembers { get; }
        ISystemLogRepository SystemLogs { get; }
        Task<int> SaveChangesAsync();
    }
}
