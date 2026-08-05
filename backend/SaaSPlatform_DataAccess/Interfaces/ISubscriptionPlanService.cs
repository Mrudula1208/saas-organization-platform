using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ISubscriptionPlanService
    {
        Task<IEnumerable<SubscriptionPlan>> GetAllAsync();
        Task<SubscriptionPlan> GetByIdAsync(Guid Id);
        Task<SubscriptionPlan>AddAsync(SubscriptionPlan subscriptionPlan);
        Task<bool>UpdateAsync(Guid Id, SubscriptionPlan subscriptionPlan);
        Task <bool>DeleteAsync(Guid Id);
    }
}
