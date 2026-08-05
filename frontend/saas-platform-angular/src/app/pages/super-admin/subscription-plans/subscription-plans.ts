import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface Plan {
  id: string;
  name: string;
  price: number;
  maxUsers: number;
  maxProjects: number;
  storageLimit: number; // in GB
  isActive: boolean;
}

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-plans.html',
  styleUrl: './subscription-plans.css',
})
export class SubscriptionPlans implements OnInit {
  plans: Plan[] = [
    { id: '1', name: 'Basic', price: 15, maxUsers: 20, maxProjects: 30, storageLimit: 2, isActive: true },
    { id: '2', name: 'Pro', price: 45, maxUsers: 40, maxProjects: 100, storageLimit: 10, isActive: true },
    { id: '3', name: 'Enterprise', price: 180, maxUsers: 180, maxProjects: 200, storageLimit: 50, isActive: true }
  ];

  isCreateModalOpen = false;
  newPlan = { name: '', price: 29, maxUsers: 25, maxProjects: 50, storageLimit: 5 };

  ngOnInit() {
    this.restorePlans();
  }

  restorePlans() {
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem('saas_plans');
      if (stored) {
        this.plans = JSON.parse(stored);
      }
    }
  }

  savePlansToStorage() {
    if (typeof window !== 'undefined') {
      localStorage.setItem('saas_plans', JSON.stringify(this.plans));
    }
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

    const planToAdd: Plan = {
      id: crypto.randomUUID(),
      name: this.newPlan.name,
      price: this.newPlan.price,
      maxUsers: this.newPlan.maxUsers,
      maxProjects: this.newPlan.maxProjects,
      storageLimit: this.newPlan.storageLimit,
      isActive: true
    };

    this.plans.push(planToAdd);
    this.savePlansToStorage();
    this.closeCreateModal();
  }

  togglePlanStatus(plan: Plan) {
    plan.isActive = !plan.isActive;
    this.savePlansToStorage();
  }

  deletePlan(id: string) {
    if (confirm('Are you sure you want to delete this subscription plan tier? This will affect new registrations.')) {
      this.plans = this.plans.filter(p => p.id !== id);
      this.savePlansToStorage();
    }
  }
}

