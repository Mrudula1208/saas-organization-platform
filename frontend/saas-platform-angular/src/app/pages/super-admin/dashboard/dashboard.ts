import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TenantService } from '../../../core/services/tenant';
import { UserService } from '../../../core/services/user';
import { SystemLogService } from '../../../core/services/system-log';
import { Tenant } from '../../../models/tenant.model';
import { User } from '../../../models/user.model';
import { SystemLog } from '../../../models/system-log.model';

export interface ActivityFeedItem {
  id: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  title: string;
  subtitle: string;
  timeAgo: string;
}

export interface TenantTableItem {
  id: string;
  name: string;
  subtitle: string;
  initials: string;
  plan: string;
  lastUsed: string;
  revenue: number;
  status: string;
  isSelected?: boolean;
}

export interface MonthlyBarData {
  month: string;
  count: number;
  bottomH: number;
  middleH: number;
  topH: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  // Dynamic KPI Metrics (loaded from database)
  totalTenants = 0;
  totalUsers = 0;
  totalProjects = 0;
  monthlyRevenue = 0;
  activeTenants = 0;

  // Filter states
  timeFilter = 'All Time';
  selectAllTenants = false;
  isLoading = true;
  lastSyncedAt: Date = new Date();

  // Dynamic Activity Feed
  activityFeed: ActivityFeedItem[] = [];

  // Dynamic Tenant Table
  tenantRows: TenantTableItem[] = [];

  // Dynamic Monthly Registration Stacked Bar Chart Data
  monthlyBars: MonthlyBarData[] = [];

  // Dynamic Chart Axis Labels
  chartY1 = 100;
  chartY2 = 80;
  chartY3 = 60;
  chartY4 = 40;
  chartY5 = 20;
  chartMonths: string[] = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'];

  // Plan price mapping for revenue calculation when not explicitly set
  private readonly planPriceMap: Record<string, number> = {
    'Basic': 15,
    'Pro': 45,
    'Enterprise': 180
  };

  constructor(
    private tenantService: TenantService,
    private userService: UserService,
    private systemLogService: SystemLogService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.isLoading = true;

    forkJoin({
      tenantRes: this.tenantService.getAll(1, 200).pipe(
        catchError((err) => {
          console.error('Failed to load tenants:', err);
          return of({ data: [], totalCount: 0, page: 1, pageSize: 200 });
        })
      ),
      userRes: this.userService.getUsers(1, 200).pipe(
        catchError((err) => {
          console.error('Failed to load users:', err);
          return of({ data: [], totalCount: 0, page: 1, pageSize: 200 });
        })
      ),
      logRes: this.systemLogService.getLogs(undefined, undefined, undefined, undefined, 1, 10).pipe(
        catchError((err) => {
          console.error('Failed to load logs:', err);
          return of({ data: [], totalCount: 0, page: 1, pageSize: 10 });
        })
      )
    }).subscribe({
      next: ({ tenantRes, userRes, logRes }) => {
        try {
          const tenants: Tenant[] = Array.isArray(tenantRes?.data) ? tenantRes.data : [];
          const rawUsers: any = (userRes as any)?.data ?? userRes;
          const users: User[] = Array.isArray(rawUsers) ? rawUsers : [];
          const logs: SystemLog[] = Array.isArray(logRes?.data) ? logRes.data : [];

          // 1. Dynamic KPI Metrics
          this.totalTenants = tenantRes?.totalCount ?? tenants.length;
          this.totalUsers = userRes?.totalCount ?? users.length;
          this.activeTenants = tenants.filter(t => t.isActive !== false && t.status !== 'Suspended').length;

          // Projects Count: aggregate projectsCount from all tenants
          this.totalProjects = tenants.reduce((sum, t) => sum + (Number(t.projectsCount) || 0), 0);

          // Monthly Revenue: dynamic calculation based on real active tenants and their tier
          this.monthlyRevenue = tenants
            .filter(t => t.isActive !== false)
            .reduce((sum, t) => {
              const explicitRevenue = Number(t.monthlyRevenue);
              if (explicitRevenue > 0) return sum + explicitRevenue;
              const planPrice = this.planPriceMap[t.plan || 'Basic'] || 15;
              return sum + planPrice;
            }, 0);

          // 2. Dynamic Tenant Management Table
          this.tenantRows = tenants.map((t, idx) => {
            const planName = t.plan || 'Basic';
            const revenue = Number(t.monthlyRevenue) > 0 ? Number(t.monthlyRevenue) : (this.planPriceMap[planName] || 15);
            return {
              id: t.id || `${idx + 1}`,
              name: t.name,
              subtitle: t.domain ? `${t.domain}.saasapp.com` : 'Tenant Workspace',
              initials: this.getInitials(t.name),
              plan: planName,
              lastUsed: this.formatRelativeTime(t.createdAt),
              revenue: revenue,
              status: t.isActive !== false ? (t.status || 'Active') : 'Suspended',
              isSelected: false
            };
          });

          // 3. Dynamic Activity Feed from System Logs and Events
          if (logs.length > 0) {
            this.activityFeed = logs.slice(0, 8).map(log => ({
              id: log.id,
              icon: this.getLogIcon(log.action),
              iconBg: 'rgba(79, 70, 229, 0.12)', // Royal blue/indigo
              iconColor: '#6366F1', // Royal blue/indigo
              title: this.formatActionTitle(log.action),
              subtitle: log.description || 'System activity logged',
              timeAgo: this.formatRelativeTime(log.createdAt)
            }));
          } else {
            // Synthesize dynamic activity from recent tenants & users
            const fallbackFeed: ActivityFeedItem[] = [];
            tenants.slice(0, 4).forEach((t, i) => {
              fallbackFeed.push({
                id: `t-${i}`,
                icon: 'corporate_fare',
                iconBg: 'rgba(79, 70, 229, 0.12)',
                iconColor: '#6366F1',
                title: `${t.name} Created`,
                subtitle: `${t.name} workspace active on ${t.plan || 'Standard'} plan`,
                timeAgo: this.formatRelativeTime(t.createdAt)
              });
            });
            users.slice(0, 4).forEach((u, i) => {
              fallbackFeed.push({
                id: `u-${i}`,
                icon: 'person',
                iconBg: 'rgba(59, 130, 246, 0.12)',
                iconColor: '#3B82F6',
                title: 'User registered',
                subtitle: `${u.fullName} registered (${u.role})`,
                timeAgo: this.formatRelativeTime(u.createdAt)
              });
            });
            this.activityFeed = fallbackFeed.slice(0, 8);
          }

          // 4. Dynamic Monthly Registrations Bar Chart
          this.monthlyBars = this.generateMonthlyBars(users);

          // 5. Dynamic Chart Y-Axis Scale
          const maxVal = Math.max(10, this.totalTenants);
          this.chartY1 = Math.round(maxVal);
          this.chartY2 = Math.round(maxVal * 0.8);
          this.chartY3 = Math.round(maxVal * 0.6);
          this.chartY4 = Math.round(maxVal * 0.4);
          this.chartY5 = Math.round(maxVal * 0.2);

          this.lastSyncedAt = new Date();
        } catch (e) {
          console.error('Error processing dashboard data:', e);
        } finally {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // Generate monthly bars dynamically from user registration dates
  private generateMonthlyBars(users: User[]): MonthlyBarData[] {
    const now = new Date();
    const months: string[] = [];
    for (let i = 4; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      months.push(d.toLocaleString('default', { month: 'short' }));
    }

    return months.map((m, idx) => {
      const count = users.filter(u => {
        if (!u.createdAt) return false;
        const d = new Date(u.createdAt);
        return d.toLocaleString('default', { month: 'short' }) === m;
      }).length;

      // Dynamic proportional height based on actual records
      const baseHeight = Math.max(12, count * 15 + (idx + 1) * 10);
      return {
        month: m,
        count: count,
        bottomH: Math.round(baseHeight * 0.4),
        middleH: Math.round(baseHeight * 0.5),
        topH: Math.round(baseHeight * 0.7)
      };
    });
  }

  // Dynamic growth curve path calculated from actual tenant count
  get growthSvgPath(): string {
    const maxVal = Math.max(10, this.totalTenants);
    const p1 = 190 - Math.round((Math.max(1, this.totalTenants * 0.2) / maxVal) * 150);
    const p2 = 190 - Math.round((Math.max(1, this.totalTenants * 0.45) / maxVal) * 150);
    const p3 = 190 - Math.round((Math.max(2, this.totalTenants * 0.7) / maxVal) * 150);
    const p4 = 190 - Math.round((this.totalTenants / maxVal) * 150);
    return `M 55 190 C 130 ${p1}, 230 ${p2}, 340 ${p3} C 410 ${p3 - 10}, 460 ${p4 + 10}, 515 ${p4}`;
  }

  get growthSvgArea(): string {
    return `${this.growthSvgPath} L 515 190 L 55 190 Z`;
  }

  toggleSelectAll() {
    this.tenantRows.forEach(t => t.isSelected = this.selectAllTenants);
  }

  onRowSelectChange() {
    this.selectAllTenants = this.tenantRows.length > 0 && this.tenantRows.every(t => t.isSelected);
  }

  formatRelativeTime(dateStr?: string): string {
    if (!dateStr) return 'Just now';
    const diffMs = Date.now() - new Date(dateStr).getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 30) return `${diffDays}d ago`;
    const diffMonths = Math.floor(diffDays / 30);
    return `${diffMonths} month${diffMonths > 1 ? 's' : ''} ago`;
  }

  getLogIcon(action?: string): string {
    if (!action) return 'info';
    const act = action.toUpperCase();
    if (act.includes('TENANT')) return 'corporate_fare';
    if (act.includes('USER') || act.includes('LOGIN')) return 'person';
    if (act.includes('PLAN') || act.includes('SUBSCRIPTION')) return 'upgrade';
    if (act.includes('PROJECT')) return 'folder';
    if (act.includes('PAYMENT') || act.includes('BILLING')) return 'payments';
    return 'notifications';
  }

  formatActionTitle(action?: string): string {
    if (!action) return 'System Event';
    return action
      .toLowerCase()
      .split('_')
      .map(w => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');
  }

  private getInitials(name: string): string {
    if (!name) return 'TN';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
}
