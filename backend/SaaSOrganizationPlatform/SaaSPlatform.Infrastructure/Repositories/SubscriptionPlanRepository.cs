using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class SubscriptionPlanRepository:ISubscriptionPlanRepository

    {
        private readonly ApplicationDbContext _context;
        public SubscriptionPlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
        {
            return await _context.SubscriptionPlans.ToListAsync();

        }

        public async Task<SubscriptionPlan> GetByIdAsync(Guid Id)
        {
            return await _context.SubscriptionPlans.FindAsync(Id);
        }

        public async Task<SubscriptionPlan> AddAsync(SubscriptionPlan subscriptionPlan)
        {
             await _context.SubscriptionPlans.AddAsync(subscriptionPlan);
            await _context.SaveChangesAsync();
            return subscriptionPlan;
        }


        public async Task<bool> UpdateAsync(SubscriptionPlan subscriptionPlan)
        {
             _context.SubscriptionPlans.Update(subscriptionPlan);
           var result = await _context.SaveChangesAsync();
            return result > 0;


            
        }

        public async Task<bool> DeleteAsync(SubscriptionPlan subscriptionPlan)
        {
             _context.Remove(subscriptionPlan);
       var result=  await _context. SaveChangesAsync();
            return result > 0;
        }
    }
}
