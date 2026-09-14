using Microsoft.EntityFrameworkCore;
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
        public static async Task Initialize(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            context.Database.EnsureCreated();

            // 1. Seed Subscription Plans
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
                    MaxUsers = 40,
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
                    MaxUsers = 180,
                    MaxProjects = 200,
                    StorageLimitMB = 51200,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // Demo accounts are only created when a password is configured.
            // Development gets these from local user secrets; production can supply
            // environment variables (Seed__AdminPassword etc.) on the first start.
            var adminPassword = configuration["Seed:AdminPassword"];
            var tenantAdminPassword = configuration["Seed:TenantAdminPassword"];
            var memberPassword = configuration["Seed:MemberPassword"];

            if (!string.IsNullOrWhiteSpace(adminPassword))
            {
                // 2. Seed System Tenant
                var systemTenantId = Guid.Parse("dddd1111-2222-3333-4444-555566667777");
                if (!await context.Tenants.AnyAsync(t => t.Id == systemTenantId))
                {
                    await context.Tenants.AddAsync(new Tenant
                    {
                        Id = systemTenantId,
                        Name = "System Tenant",
                        Domain = "system.saasapp.com",
                        ContactEmail = "system@saas.com",
                        ContactPhone = "123-456-7890",
                        SubscriptionPlanId = enterprisePlanId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }

                // 3. Seed Super Admin User
                var superAdminId = Guid.Parse("aaaa1111-2222-3333-4444-555566667777");
                if (!await context.Users.AnyAsync(u => u.Id == superAdminId))
                {
                    await context.Users.AddAsync(new User
                    {
                        Id = superAdminId,
                        FullName = "JD Dewifrav", // Matches frontend active username
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
            }
            else
            {
                Console.WriteLine("Seed:AdminPassword is not configured - the super admin account was not created.");
            }

            if (!string.IsNullOrWhiteSpace(tenantAdminPassword) && !string.IsNullOrWhiteSpace(memberPassword))
            {
                // 4. Seed test Tenant: Acme Corp
                var acmeTenantId = Guid.Parse("11112222-3333-4444-5555-666677778888");
                if (!await context.Tenants.AnyAsync(t => t.Id == acmeTenantId))
                {
                    await context.Tenants.AddAsync(new Tenant
                    {
                        Id = acmeTenantId,
                        Name = "Acme Corp",
                        Domain = "acme.saasapp.com",
                        ContactEmail = "admin@acme.com",
                        ContactPhone = "555-0199",
                        SubscriptionPlanId = proPlanId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }

                // 5. Seed test Tenant Admin for Acme Corp
                var acmeAdminId = Guid.Parse("22223333-4444-5555-6666-777788889999");
                if (!await context.Users.AnyAsync(u => u.Id == acmeAdminId))
                {
                    await context.Users.AddAsync(new User
                    {
                        Id = acmeAdminId,
                        FullName = "Acme Administrator",
                        Email = "tenant@acme.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(tenantAdminPassword),
                        Role = "TenantAdmin",
                        TenantId = acmeTenantId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }

                // 6. Seed Member User for Acme Corp
                var acmeMemberId = Guid.Parse("33334444-5555-6666-7777-888899990000");
                if (!await context.Users.AnyAsync(u => u.Id == acmeMemberId))
                {
                    await context.Users.AddAsync(new User
                    {
                        Id = acmeMemberId,
                        FullName = "Jann Sanner",
                        Email = "member@acme.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(memberPassword),
                        Role = "Member",
                        TenantId = acmeTenantId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                Console.WriteLine("Seed tenant passwords are not configured - demo tenant users were not created.");
            }

            // 7. Normalize any legacy role names so all users use the app's role naming
            var legacyRoles = await context.Users.Where(u => u.Role == "Admin" || u.Role == "TenantUSer").ToListAsync();
            foreach (var legacyUser in legacyRoles)
            {
                if (legacyUser.Role == "Admin")
                {
                    legacyUser.Role = "SuperAdmin";
                }
                else if (legacyUser.Role == "TenantUSer")
                {
                    legacyUser.Role = "Member";
                }
            }
            if (legacyRoles.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
