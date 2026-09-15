using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using SaaSPlatform.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure
{
    public static class SeedData
    {
        public static Task Initialize(ApplicationDbContext context) => Initialize(context, null);

        public static async Task Initialize(ApplicationDbContext context, IConfiguration? configuration)
        {
            context.Database.EnsureCreated();

            // =========================================================================
            // 1. Subscription Plans (System Reference Data)
            // =========================================================================
            var basicPlanId = Guid.Parse("bbbb1111-2222-3333-4444-555566667777");
            var proPlanId = Guid.Parse("cccc1111-2222-3333-4444-555566667777");
            var enterprisePlanId = Guid.Parse("eeee1111-2222-3333-4444-555566667777");

            if (!await context.SubscriptionPlans.AnyAsync(p => p.Id == basicPlanId))
            {
                await context.SubscriptionPlans.AddAsync(new SubscriptionPlan
                {
                    Id = basicPlanId,
                    Name = "Basic",
                    Price = 15,
                    MaxUsers = 20,
                    MaxProjects = 30,
                    StorageLimitMB = 2048,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.SubscriptionPlans.AnyAsync(p => p.Id == proPlanId))
            {
                await context.SubscriptionPlans.AddAsync(new SubscriptionPlan
                {
                    Id = proPlanId,
                    Name = "Pro",
                    Price = 45,
                    MaxUsers = 50,
                    MaxProjects = 100,
                    StorageLimitMB = 10240,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.SubscriptionPlans.AnyAsync(p => p.Id == enterprisePlanId))
            {
                await context.SubscriptionPlans.AddAsync(new SubscriptionPlan
                {
                    Id = enterprisePlanId,
                    Name = "Enterprise",
                    Price = 180,
                    MaxUsers = 250,
                    MaxProjects = 500,
                    StorageLimitMB = 51200,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // =========================================================================
            // 2. System Host Tenant (Required by User.TenantId Foreign Key Constraint)
            // =========================================================================
            var systemTenantId = Guid.Parse("dddd1111-2222-3333-4444-555566667777");
            if (!await context.Tenants.AnyAsync(t => t.Id == systemTenantId))
            {
                await context.Tenants.AddAsync(new Tenant
                {
                    Id = systemTenantId,
                    Name = "Platform Operations",
                    Domain = "system.saasapp.com",
                    ContactEmail = "admin@saas.com",
                    ContactPhone = "+1 (800) 555-0100",
                    SubscriptionPlanId = enterprisePlanId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 3. Super Admin User (Single Root Administrator Account)
            // =========================================================================
            var adminPassword = configuration?["Seed:AdminPassword"] ?? "admin123";
            var superAdminId = Guid.Parse("aaaa1111-2222-3333-4444-555566667777");
            var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Id == superAdminId || u.Email == "admin@saas.com");

            if (existingAdmin == null)
            {
                await context.Users.AddAsync(new User
                {
                    Id = superAdminId,
                    FullName = "System Administrator",
                    Email = "admin@saas.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = "SuperAdmin",
                    TenantId = systemTenantId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            else
            {
                existingAdmin.FullName = "System Administrator";
                existingAdmin.Role = "SuperAdmin";
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                await context.SaveChangesAsync();
            }
        }
    }
}
