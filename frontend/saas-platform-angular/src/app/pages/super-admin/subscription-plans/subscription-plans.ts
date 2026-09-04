import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubscriptionPlanService } from '../../../core/services/subscription-plan';
import { SubscriptionPlan } from '../../../models/subscription.model';

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-plans.html',
  styleUrl: './subscription-plans.css',
})
export class SubscriptionPlans implements OnInit {
  plans: SubscriptionPlan[] = [];

  loading = false;
  saving = false;
  errorMessage = '';

  isCreateModalOpen = false;
  newPlan = { name: '', price: 29, maxUsers: 25, maxProjects: 50, storageLimit: 5 };

  constructor(private planService: SubscriptionPlanService) {}

  ngOnInit() {
    this.loadPlans();
  }

  loadPlans() {
    this.loading = true;
    this.errorMessage = '';

    this.planService.getPlans().subscribe({
      next: (data) => {
        this.plans = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load subscription plans. Please try again later.';
        this.plans = [];
      },
    });
  }

  openCreateModal() {
    this.newPlan = { name: '', price: 29, maxUsers: 25, maxProjects: 50, storageLimit: 5 };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  saveNewPlan() {
    if (!this.newPlan.name || this.newPlan.price < 0) return;

    this.saving = true;
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
        this.loadPlans();
        this.closeCreateModal();
      },
      error: () => {
        this.saving = false;
        this.errorMessage = 'Failed to create plan. Please try again.';
      },
    });
  }

  togglePlanStatus(plan: SubscriptionPlan) {
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
        },
        error: () => {
          this.errorMessage = 'Failed to update plan status.';
        },
      });
  }

  deletePlan(id: string) {
    if (confirm('Are you sure you want to delete this subscription plan tier? This will affect new registrations.')) {
      this.planService.deletePlan(id).subscribe({
        next: () => {
          this.plans = this.plans.filter((p) => p.id !== id);
        },
        error: () => {
          this.errorMessage = 'Failed to delete plan.';
        },
      });
    }
  }
}