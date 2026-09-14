using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly ISystemLogRepository _systemLogs;

        public SubscriptionPlanService(ISubscriptionPlanRepository subscriptionPlanRepository, ISystemLogRepository systemLogs)
        {
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _systemLogs = systemLogs;
        }


        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
        {
            return await _subscriptionPlanRepository.GetAllAsync();

        }

        public async Task<SubscriptionPlan> GetByIdAsync(Guid Id)
        {
            return await _subscriptionPlanRepository.GetByIdAsync(Id);
        }

        public async Task<SubscriptionPlan> AddAsync(SubscriptionPlan subscriptionPlan, Guid? userId = null)
        {
            if (string.IsNullOrEmpty(subscriptionPlan.Name))
            {
                throw new Exception("Subscription Plan is required");

            }

            var created = await _subscriptionPlanRepository.AddAsync(subscriptionPlan);

            // Subscription plans are platform-level entities: tenant id stays global (Guid.Empty).
            await _systemLogs.LogAsync(
                "SUBSCRIPTION_PLAN_CREATED",
                $"Subscription plan '{created.Name}' created (price {created.Price.ToString(CultureInfo.InvariantCulture)}).",
                userId, null);

            return created;
        }



        public async Task<bool> UpdateAsync(Guid Id, SubscriptionPlan subscriptionPlan, Guid? userId = null)
        {
            var existing = await _subscriptionPlanRepository.GetByIdAsync(Id);
            if (existing == null)
            {
                return false;
            }
            existing.Name = subscriptionPlan.Name;
            existing.Price = subscriptionPlan.Price;
            existing.MaxUsers = subscriptionPlan.MaxUsers;
            existing.MaxProjects = subscriptionPlan.MaxProjects;
            existing.StorageLimitMB = subscriptionPlan.StorageLimitMB;
            existing.IsActive = subscriptionPlan.IsActive;

            var result = await _subscriptionPlanRepository.UpdateAsync(existing);
            if (result)
            {
                await _systemLogs.LogAsync(
                    "SUBSCRIPTION_PLAN_UPDATED",
                    $"Subscription plan '{existing.Name}' updated (price {existing.Price.ToString(CultureInfo.InvariantCulture)}, active: {existing.IsActive}).",
                    userId, null);
            }
            return result;
        }



        public async Task<bool> DeleteAsync(Guid Id, Guid? userId = null)
        {
            var plan = await _subscriptionPlanRepository.GetByIdAsync(Id);
            if (plan == null)
                return false;

            var result = await _subscriptionPlanRepository.DeleteAsync(plan);
            if (result)
            {
                await _systemLogs.LogAsync(
                    "SUBSCRIPTION_PLAN_DELETED",
                    $"Subscription plan '{plan.Name}' deleted.",
                    userId, null);
            }
            return result;
        }

    }
}
