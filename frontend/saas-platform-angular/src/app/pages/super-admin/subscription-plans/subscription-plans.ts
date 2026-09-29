import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SubscriptionPlanService } from '../../../core/services/subscription-plan';
import { TenantService } from '../../../core/services/tenant';
import { SubscriptionPlan } from '../../../models/subscription.model';
import { Tenant } from '../../../models/tenant.model';

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-plans.html',
  styleUrl: './subscription-plans.css',
})
export class SubscriptionPlans implements OnInit {
  plans: SubscriptionPlan[] = [];

  // Dynamic KPI Metrics
  totalPlans = 0;
  activeSubscribers = 0;
  planConversionRate = 0;
  planRevenue = 0;
  expiredSubscriptions = 0;

  loading = false;
  saving = false;
  errorMessage = '';

  isCreateModalOpen = false;
  newPlan = { name: '', price: 29, maxUsers: 25, maxProjects: 50, storageLimit: 5 };

  private readonly defaultPriceMap: Record<string, number> = {
    'Basic': 15,
    'Pro': 45,
    'Enterprise': 180
  };

  constructor(
    private planService: SubscriptionPlanService,
    private tenantService: TenantService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadPlansAndMetrics();
  }

  loadPlansAndMetrics() {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      plansRes: this.planService.getPlans().pipe(
        catchError(() => of([]))
      ),
      tenantsRes: this.tenantService.getAll(1, 200).pipe(
        catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 200 }))
      )
    }).subscribe({
      next: ({ plansRes, tenantsRes }) => {
        this.plans = plansRes || [];
        this.totalPlans = this.plans.length;

        const tenants: Tenant[] = Array.isArray(tenantsRes?.data) ? tenantsRes.data : [];
        const totalTenants = tenantsRes?.totalCount ?? tenants.length;

        // 100% Dynamic metrics from real database
        this.activeSubscribers = tenants.filter(t => t.isActive !== false && t.status !== 'Suspended').length;
        this.expiredSubscriptions = tenants.filter(t => t.isActive === false || t.status === 'Suspended').length;
        this.planConversionRate = totalTenants > 0 ? Math.round((this.activeSubscribers / totalTenants) * 100) : 0;

        // Dynamic revenue
        this.planRevenue = tenants
          .filter(t => t.isActive !== false)
          .reduce((sum, t) => {
            const explicit = Number(t.monthlyRevenue);
            if (explicit > 0) return sum + explicit;
            const matchedPlan = this.plans.find(p => p.name.toLowerCase() === (t.plan || '').toLowerCase());
            return sum + (matchedPlan ? matchedPlan.price : (this.defaultPriceMap[t.plan || 'Basic'] || 15));
          }, 0);

        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load subscription plans. Please try again.';
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  openCreateModal() {
    this.newPlan = { name: '', price: 29, maxUsers: 25, maxProjects: 50, storageLimit: 5 };
    this.isCreateModalOpen = true;
    this.cdr.markForCheck();
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
    this.cdr.markForCheck();
  }

  saveNewPlan() {
    if (!this.newPlan.name || this.newPlan.price < 0) return;

    this.saving = true;
    this.cdr.markForCheck();
    this.planService
      .createPlan({
        name: this.newPlan.name,
        price: this.newPlan.price,
        maxUsers: this.newPlan.maxUsers,
        maxProjects: this.newPlan.maxProjects,
        storageLimitMB: this.newPlan.storageLimit * 1024,
      })
      .subscribe({
        next: () => {
          this.saving = false;
          this.loadPlansAndMetrics();
          this.closeCreateModal();
        },
        error: () => {
          this.saving = false;
          this.errorMessage = 'Failed to create plan. Please try again.';
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
      });
  }

  togglePlanStatus(plan: SubscriptionPlan) {
    if (plan.isActive) {
      if (!confirm(`Are you sure you want to suspend the '${plan.name}' tier? New organizations will not be able to choose this tier.`)) {
        return;
      }
    }
    this.planService
      .updatePlan(plan.id, {
        name: plan.name,
        price: plan.price,
        maxUsers: plan.maxUsers,
        maxProjects: plan.maxProjects,
        storageLimitMB: plan.storageLimitMB,
        isActive: !plan.isActive,
      })
      .subscribe({
        next: () => {
          plan.isActive = !plan.isActive;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Failed to update plan status.';
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
      });
  }

  deletePlan(id: string) {
    if (confirm('Are you sure you want to delete this subscription plan tier? This will affect new registrations.')) {
      this.planService.deletePlan(id).subscribe({
        next: () => {
          this.plans = this.plans.filter((p) => p.id !== id);
          this.totalPlans = this.plans.length;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Failed to delete plan.';
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
      });
    }
  }
}