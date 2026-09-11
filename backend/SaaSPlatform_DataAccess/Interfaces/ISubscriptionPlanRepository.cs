using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface ISubscriptionPlanRepository
    {
        Task<IEnumerable<SubscriptionPlan>> GetAllAsync();
        Task<SubscriptionPlan> GetByIdAsync(Guid Id);
        Task<SubscriptionPlan> AddAsync(SubscriptionPlan subscriptionPlan);
        Task<bool> UpdateAsync(SubscriptionPlan subscriptionPlan);
        Task<bool> DeleteAsync(SubscriptionPlan subscriptionPlan);
    }
}
