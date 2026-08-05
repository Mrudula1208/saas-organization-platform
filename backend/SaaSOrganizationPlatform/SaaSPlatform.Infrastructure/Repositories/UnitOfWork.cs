using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(
            ApplicationDbContext context,
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IProjectRepository projectRepository,
            ITaskRepository taskRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository,
            IPaymentRepository paymentRepository,
            IReportRepository reportRepository,
            IProjectMemberRepository projectMemberRepository,
            ISystemLogRepository systemLogRepository)
        {
            _context = context;
            Users = userRepository;
            Tenants = tenantRepository;
            Projects = projectRepository;
            Tasks = taskRepository;
            SubscriptionPlans = subscriptionPlanRepository;
            Payments = paymentRepository;
            Reports = reportRepository;
            ProjectMembers = projectMemberRepository;
            SystemLogs = systemLogRepository;
        }

        public IUserRepository Users { get; }
        public ITenantRepository Tenants { get; }
        public IProjectRepository Projects { get; }
        public ITaskRepository Tasks { get; }
        public ISubscriptionPlanRepository SubscriptionPlans { get; }
        public IPaymentRepository Payments { get; }
        public IReportRepository Reports { get; }
        public IProjectMemberRepository ProjectMembers { get; }
        public ISystemLogRepository SystemLogs { get; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
