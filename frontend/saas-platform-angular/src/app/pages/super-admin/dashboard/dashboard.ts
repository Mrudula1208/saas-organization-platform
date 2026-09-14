import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TenantService } from '../../../core/services/tenant';
import { UserService } from '../../../core/services/user';
import { Tenant } from '../../../models/tenant.model';
import { User } from '../../../models/user.model';

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
    // Aggregates read one wide page from the API (the API caps page size).
    this.tenantService.getAll(1, 200).subscribe({
      next: (res) => {
        const tenants = res.data;
        this.totalTenants = res.totalCount;
        this.recentTenants = tenants.slice(0, 5);
        
        // Compute MRR and Plan distributions
        this.monthlyRevenue = tenants.reduce((sum: number, t: Tenant) => sum + (t.monthlyRevenue || 0), 0);
        this.basicPlanCount = tenants.filter((t: Tenant) => t.plan === 'Basic').length;
        this.proPlanCount = tenants.filter((t: Tenant) => t.plan === 'Pro').length;
        this.enterprisePlanCount = tenants.filter((t: Tenant) => t.plan === 'Enterprise').length;
        
        this.userService.getUsers(1, 200).subscribe({
          next: (userRes) => {
            const users = userRes.data;
            this.activeUsers = users.filter((u: User) => u.isActive).length;
            this.recentUsers = users.slice(0, 5);
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

