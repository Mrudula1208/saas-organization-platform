import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubscriptionPlanService, Plan } from '../../../core/services/subscription-plan';

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-plans.html',
  styleUrl: './subscription-plans.css',
})
export class SubscriptionPlans implements OnInit {
  plans: Plan[] = [];

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
    this.planService.createPlan(this.newPlan).subscribe({
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

  togglePlanStatus(plan: Plan) {
    this.planService
      .updatePlan(plan.id, {
        name: plan.name,
        price: plan.price,
        maxUsers: plan.maxUsers,
        maxProjects: plan.maxProjects,
        storageLimit: plan.storageLimit,
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