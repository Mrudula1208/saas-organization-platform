import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
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
    
    forkJoin({
      tenantRes: this.tenantService.getAll(1, 200).pipe(catchError(err => {
        console.error('Failed to load tenants:', err);
        return of({ data: [], totalCount: 0, page: 1, pageSize: 200 });
      })),
      userRes: this.userService.getUsers(1, 200).pipe(catchError(err => {
        console.error('Failed to load users:', err);
        return of({ data: [], totalCount: 0, page: 1, pageSize: 200 });
      }))
    }).subscribe({
      next: ({ tenantRes, userRes }) => {
        try {
          const tenants: Tenant[] = Array.isArray(tenantRes?.data) ? tenantRes.data : [];
          this.totalTenants = tenantRes?.totalCount ?? tenants.length;
          this.recentTenants = tenants.slice(0, 5);
          
          this.monthlyRevenue = tenants.reduce((sum: number, t: any) => sum + (Number(t?.monthlyRevenue) || 0), 0);
          this.basicPlanCount = tenants.filter((t: any) => t?.plan === 'Basic').length;
          this.proPlanCount = tenants.filter((t: any) => t?.plan === 'Pro').length;
          this.enterprisePlanCount = tenants.filter((t: any) => t?.plan === 'Enterprise').length;
          
          const rawUsers: any = (userRes as any)?.data ?? userRes;
          const users: User[] = Array.isArray(rawUsers) ? rawUsers : [];
          this.activeUsers = users.filter((u: any) => u?.isActive !== false).length;
          this.recentUsers = users.slice(0, 5);
        } catch (e) {
          console.error('Error processing dashboard data:', e);
        } finally {
          this.isLoading = false;
        }
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}

