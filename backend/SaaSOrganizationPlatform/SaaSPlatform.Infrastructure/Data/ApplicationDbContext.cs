using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using User = SaaSPlatform_Model.User;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Report = SaaSPlatform.Domain.Entities.Report;
using Payment = SaaSPlatform.Domain.Entities.Payment;

namespace SaaSPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<PlatformSetting> PlatformSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔥 USER → TENANT
            modelBuilder.Entity<User>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 PROJECT → TENANT
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Tenant)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 PROJECT → OWNER (USER)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 PROJECT MEMBER → PROJECT + USER (unique per project/user)
            // EF's foreign-key convention supplies the UserId lookup. The
            // composite index also serves project lookups and prevents duplicate memberships.
            modelBuilder.Entity<ProjectMember>()
                .HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            // 🔥 TASK → PROJECT
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 TASK → USER
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant, project, user and email are the common predicates in the
            // list, authentication and ownership queries. These composites
            // target the actual tenant-scoped list patterns; EF removes only
            // redundant single-column prefixes when the composite is sufficient.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email);

            modelBuilder.Entity<Project>()
                .HasIndex(p => new { p.TenantId, p.IsDeleted, p.CreatedAt });

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => new { t.TenantId, t.Status, t.CreatedAt });

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => new { t.ProjectId, t.Status, t.CreatedAt });

            // Notification is tenant-wide in the current domain (there is no
            // Notification.UserId column); this index covers tenant/unread/date
            // lookups without inventing a recipient model.
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.TenantId, n.IsRead, n.CreatedAt });

            // System log pages filter by tenant and a date range.
            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => new { l.TenantId, l.CreatedAt });

            // Billing history and revenue windows are tenant/date lookups.
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.TenantId, p.PaymentDate });

            // ProjectMember currently has no TenantId column: tenant scope is
            // derived through Project. Its existing unique (ProjectId, UserId)
            // index is retained and already covers the project lookup.

            // 🔥 TENANT → SUBSCRIPTION PLAN
            modelBuilder.Entity<Tenant>()
                .HasOne<SubscriptionPlan>()
                .WithMany()
                .HasForeignKey(t => t.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 TENANT SETTINGS → notification preferences default to enabled
            modelBuilder.Entity<Tenant>()
                .Property(t => t.EmailNotificationsEnabled)
                .HasDefaultValue(true);

            modelBuilder.Entity<Tenant>()
                .Property(t => t.InAppNotificationsEnabled)
                .HasDefaultValue(true);

            // 🔥 PAYMENT → TENANT
            modelBuilder.Entity<Payment>()
                .HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 REPORT → TENANT
            modelBuilder.Entity<Report>()
                .HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 NOTIFICATION → TENANT
            modelBuilder.Entity<Notification>()
                .HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(n => n.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 SYSTEM LOG → USER
            modelBuilder.Entity<SystemLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 💰 DECIMAL FIXES
            modelBuilder.Entity<SubscriptionPlan>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}