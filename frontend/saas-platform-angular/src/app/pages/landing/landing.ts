import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SubscriptionPlanService } from '../../core/services/subscription-plan';
import { SettingsService } from '../../core/services/settings';
import { SubscriptionPlan } from '../../models/subscription.model';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.html',
  styleUrl: './landing.css'
})
export class LandingPage implements OnInit {
  isAnnual = false;
  plans: SubscriptionPlan[] = [];
  platformName = 'SaaS Platform';
  supportEmail = 'enterprise@saasplatform.io';
  isLoadingPlans = false;

  constructor(
    private subPlanService: SubscriptionPlanService,
    private settingsService: SettingsService
  ) {}

  ngOnInit() {
    this.loadPlans();
    this.loadPublicConfig();
  }

  loadPlans() {
    this.isLoadingPlans = true;
    this.subPlanService.getPlans().subscribe({
      next: (data) => {
        this.isLoadingPlans = false;
        if (Array.isArray(data) && data.length > 0) {
          this.plans = data.filter(p => p.isActive);
        }
      },
      error: () => {
        this.isLoadingPlans = false;
      }
    });
  }

  loadPublicConfig() {
    this.settingsService.getPublicConfig().subscribe({
      next: (config) => {
        if (config) {
          if (config.platformName) this.platformName = config.platformName;
          if (config.supportEmail) this.supportEmail = config.supportEmail;
        }
      },
      error: () => {}
    });
  }

  getPlanPrice(planName: string, defaultPrice: number): number {
    const found = this.plans.find(p => p.name.toLowerCase() === planName.toLowerCase());
    const base = found ? found.price : defaultPrice;
    return this.isAnnual ? Math.round(base * 0.8) : base;
  }

  getAnnualYearly(planName: string, defaultPrice: number): number {
    return this.getPlanPrice(planName, defaultPrice) * 12;
  }

  getPlanLimit(planName: string, field: 'users' | 'projects' | 'storage', fallback: number): number {
    const found = this.plans.find(p => p.name.toLowerCase() === planName.toLowerCase());
    if (!found) return fallback;
    if (field === 'users') return found.maxUsers;
    if (field === 'projects') return found.maxProjects;
    if (field === 'storage') return Math.round(found.storageLimitMB / 1024) || fallback;
    return fallback;
  }

  toggleBilling(annual: boolean) {
    this.isAnnual = annual;
  }
  
  submitContact(event: Event) {
    event.preventDefault();
    alert('Thank you for contacting us! A platform specialist will get back to you shortly.');
    const form = event.target as HTMLFormElement;
    form.reset();
  }
}
