import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TenantService, Tenant } from '../../../core/services/tenant';
import { UserService, User } from '../../../core/services/user';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  totalTenants = 0;
  activeUsers = 0;
  monthlyRevenue = 0;
  basicPlanCount = 0;
  proPlanCount = 0;
  enterprisePlanCount = 0;
  
  recentTenants: Tenant[] = [];
  recentUsers: User[] = [];
  
  isLoading = true;

  constructor(
    private tenantService: TenantService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.isLoading = true;
    
    // Fetch both tenants and users
    this.tenantService.getAll().subscribe({
      next: (tenants: Tenant[]) => {
        this.totalTenants = tenants.length;
        this.recentTenants = tenants.sort((a: Tenant, b: Tenant) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 5);
        
        // Compute MRR and Plan distributions
        this.monthlyRevenue = tenants.reduce((sum: number, t: Tenant) => sum + (t.monthlyRevenue || 0), 0);
        this.basicPlanCount = tenants.filter((t: Tenant) => t.plan === 'Basic').length;
        this.proPlanCount = tenants.filter((t: Tenant) => t.plan === 'Pro').length;
        this.enterprisePlanCount = tenants.filter((t: Tenant) => t.plan === 'Enterprise').length;
        
        this.userService.getUsers().subscribe({
          next: (users: User[]) => {
            this.activeUsers = users.filter((u: User) => u.isActive).length;
            this.recentUsers = users.sort((a: User, b: User) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 5);
            this.isLoading = false;
          },
          error: () => {
            this.isLoading = false;
          }
        });
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}

