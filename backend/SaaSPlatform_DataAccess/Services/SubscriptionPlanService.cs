using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {

        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        public SubscriptionPlanService(ISubscriptionPlanRepository subscriptionPlanRepository)
        {
            _subscriptionPlanRepository = subscriptionPlanRepository;
        }


        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
        {
            return await _subscriptionPlanRepository.GetAllAsync();

        }

        public async Task<SubscriptionPlan> GetByIdAsync(Guid Id)
        {
            return await _subscriptionPlanRepository.GetByIdAsync(Id);
        }

        public async Task<SubscriptionPlan> AddAsync(SubscriptionPlan subscriptionPlan)
        {
            if (string.IsNullOrEmpty(subscriptionPlan.Name))
            {
                throw new Exception("Subscription Plan is required");

            }
            return await _subscriptionPlanRepository.AddAsync(subscriptionPlan);


        }



        public async Task<bool> UpdateAsync(Guid Id, SubscriptionPlan subscriptionPlan)
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

            return await _subscriptionPlanRepository.UpdateAsync(existing);
        }



        public async Task<bool> DeleteAsync(Guid Id)
        {
            var plan = await _subscriptionPlanRepository.GetByIdAsync(Id);
            if (plan == null)
                return false;
            return await _subscriptionPlanRepository.DeleteAsync(plan);

        }

    }
}