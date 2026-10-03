import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TenantService } from '../../../core/services/tenant';
import { SystemLogService } from '../../../core/services/system-log';
import { Tenant } from '../../../models/tenant.model';
import { getErrorMessage, formatRelativeTime } from '../../../core/helpers';

export interface TenantActivityItem {
  id: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  title: string;
  subtitle: string;
  timeAgo: string;
}

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './tenants.html',
  styleUrl: './tenants.css',
})
export class Tenants implements OnInit, OnDestroy {
  filteredTenants: Tenant[] = [];

  // Real-Time Activity Feed States
  isRefreshingActivity = false;
  private pollIntervalSub: any = null;

  // Search & Filter
  searchQuery = '';
  planFilter = '';
  timeFilter = 'All Filters';
  showFilterMenu = false;
  chartMode = 'Interactive';
  showChartModeMenu = false;
  errorMessage = '';
  selectAll = false;

  // Server-side pagination
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = true;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Modals state
  isCreateModalOpen = false;
  isEditModalOpen = false;
  isViewModalOpen = false;

  // Form states
  newTenant = { name: '', plan: 'Basic', contactEmail: '', domain: '' };
  selectedTenant: Tenant | null = null;
  editTenantForm = { id: '', name: '', plan: '', contactEmail: '', domain: '', status: '' };

  // Activity Feed
  activityFeed: TenantActivityItem[] = [];
  constructor(
    private tenantService: TenantService,
    private systemLogService: SystemLogService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadTenants();
    this.loadActivityFeed();
    this.startRealtimePolling();
  }

  ngOnDestroy() {
    if (this.pollIntervalSub) {
      clearInterval(this.pollIntervalSub);
      this.pollIntervalSub = null;
    }
  }

  startRealtimePolling() {
    this.pollIntervalSub = setInterval(() => {
      this.pollActivityFeed();
    }, 5000);
  }

  pollActivityFeed() {
    this.systemLogService.getLogs(undefined, undefined, undefined, undefined, 1, 8).pipe(
      catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 8 }))
    ).subscribe((res) => {
      const logs = (res as any)?.data ?? (res as any)?.Data ?? (Array.isArray(res) ? res : []);
      if (Array.isArray(logs) && logs.length > 0) {
        this.updateActivityFeed(logs);
        this.cdr.markForCheck();
      }
    });
  }

  refreshActivityFeed() {
    this.isRefreshingActivity = true;
    this.cdr.markForCheck();
    this.systemLogService.getLogs(undefined, undefined, undefined, undefined, 1, 8).pipe(
      catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 8 }))
    ).subscribe((res) => {
      const logs = (res as any)?.data ?? (res as any)?.Data ?? (Array.isArray(res) ? res : []);
      if (Array.isArray(logs)) {
        this.updateActivityFeed(logs);
      }
      this.isRefreshingActivity = false;
      this.cdr.markForCheck();
    });
  }

  private updateActivityFeed(logs: any[]) {
    this.activityFeed = logs.map(log => {
      const style = this.getLogStyle(log.action);
      return {
        id: log.id,
        icon: style.icon,
        iconBg: style.bg,
        iconColor: style.color,
        title: this.formatActionTitle(log.action),
        subtitle: log.description || 'System event recorded',
        timeAgo: this.formatRelativeTime(log.createdAt)
      };
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get displayTotalCount(): number {
    return this.totalCount;
  }

  get activeCount(): number {
    return this.filteredTenants.filter(t => t.isActive !== false && t.status !== 'Suspended').length;
  }

  get newTenants30d(): number {
    const thirtyDaysAgo = Date.now() - 30 * 24 * 60 * 60 * 1000;
    return this.filteredTenants.filter(t => t.createdAt && new Date(t.createdAt).getTime() >= thirtyDaysAgo).length;
  }

  get pendingCount(): number {
    return this.filteredTenants.filter(t => t.isActive === false || t.status === 'Suspended' || t.status === 'Awaited').length;
  }

  get avgProjectsPerTenant(): string {
    if (this.totalCount === 0) return '1.0';
    const totalProjects = this.filteredTenants.reduce((sum, t) => sum + (Number(t.projectsCount) || 0), 0);
    const avg = totalProjects / Math.max(1, this.filteredTenants.length);
    return avg > 0 ? avg.toFixed(1) : '1.0';
  }

  get basicPlanCount(): number {
    return this.filteredTenants.filter(t => t.plan === 'Basic').length;
  }

  get proPlanCount(): number {
    return this.filteredTenants.filter(t => t.plan === 'Pro').length;
  }

  get enterprisePlanCount(): number {
    return this.filteredTenants.filter(t => t.plan === 'Enterprise').length;
  }

  // Smooth SVG curves matching the Tenant Growth Over Time card in the mockup
  // Cyan curve with glowing gradient fill underneath
  readonly growthSvgPathCyan = 'M 45 190 C 110 130, 180 85, 270 95 C 340 102, 420 50, 520 28';
  readonly growthSvgAreaCyan = 'M 45 190 C 110 130, 180 85, 270 95 C 340 102, 420 50, 520 28 L 520 190 L 45 190 Z';
  
  // Purple curve
  readonly growthSvgPathPurple = 'M 45 155 C 130 115, 210 150, 310 125 C 390 105, 460 130, 520 80';

  // Gold / Amber curve
  readonly growthSvgPathAmber = 'M 45 180 C 120 170, 200 130, 290 145 C 370 158, 440 120, 520 105';

  loadTenants() {
    this.loading = true;
    this.errorMessage = '';
    this.tenantService.getAll(this.page, this.pageSize, this.searchQuery, this.planFilter).pipe(
      catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 20 }))
    ).subscribe({
      next: (res) => {
        this.filteredTenants = res?.data ?? [];
        this.totalCount = res?.totalCount ?? this.filteredTenants.length;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.filteredTenants = [];
        this.totalCount = 0;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  loadActivityFeed() {
    this.systemLogService.getLogs(undefined, undefined, undefined, undefined, 1, 8).pipe(
      catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 8 }))
    ).subscribe({
      next: (res) => {
        const logs = (res as any)?.data ?? (res as any)?.Data ?? (Array.isArray(res) ? res : []);
        if (Array.isArray(logs) && logs.length > 0) {
          this.updateActivityFeed(logs);
        } else {
          this.activityFeed = [];
        }
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.activityFeed = [];
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  getInitials(name: string): string {
    if (!name) return 'AC';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }

  formatRelativeTime(dateStr?: string | Date): string {
    return formatRelativeTime(dateStr);
  }

  private getLogStyle(action?: string): { icon: string; bg: string; color: string } {
    if (!action) return { icon: 'bolt', bg: 'rgba(59, 130, 246, 0.12)', color: '#3B82F6' };
    const act = action.toUpperCase();
    if (act.includes('LOGIN_SUCCESS')) {
      return { icon: 'login', bg: 'rgba(16, 185, 129, 0.12)', color: '#10B981' };
    }
    if (act.includes('LOGIN_FAILED') || act.includes('FAIL')) {
      return { icon: 'error', bg: 'rgba(239, 68, 68, 0.12)', color: '#EF4444' };
    }
    if (act.includes('TENANT_CREATED') || act.includes('TENANT')) {
      return { icon: 'corporate_fare', bg: 'rgba(37, 99, 235, 0.12)', color: '#2563EB' };
    }
    if (act.includes('USER_REGISTERED') || act.includes('USER')) {
      return { icon: 'person_add', bg: 'rgba(79, 70, 229, 0.12)', color: '#6366F1' };
    }
    if (act.includes('PLAN') || act.includes('UPGRAD')) {
      return { icon: 'upgrade', bg: 'rgba(217, 119, 6, 0.12)', color: '#D97706' };
    }
    return { icon: 'notifications', bg: 'rgba(6, 182, 212, 0.12)', color: '#06B6D4' };
  }

  private getLogIcon(action?: string): string {
    return this.getLogStyle(action).icon;
  }

  private formatActionTitle(action?: string): string {
    if (!action) return 'System Action';
    return action
      .toLowerCase()
      .split('_')
      .map(w => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');
  }

  onSearch() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadTenants();
    }, 400);
  }

  onFilterChange() {
    this.page = 1;
    this.loadTenants();
  }

  setTimeFilter(filter: string) {
    this.timeFilter = filter;
    this.showFilterMenu = false;
    this.loadTenants();
  }

  setChartMode(mode: string) {
    this.chartMode = mode;
    this.showChartModeMenu = false;
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadTenants();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadTenants();
    }
  }

  // CREATE
  openCreateModal() {
    this.newTenant = { name: '', plan: 'Basic', contactEmail: '', domain: '' };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  onNewTenantNameChange() {
    if (this.newTenant.name) {
      this.newTenant.domain = `${this.newTenant.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`;
    } else {
      this.newTenant.domain = '';
    }
  }

  saveNewTenant() {
    if (!this.newTenant.name || !this.newTenant.contactEmail) return;

    this.tenantService.create(this.newTenant).subscribe({
      next: (created) => {
        if (created) {
          this.filteredTenants.unshift(created);
          this.totalCount++;
        }
        this.loadTenants();
        this.closeCreateModal();
      },
      error: () => {
        // Optimistic local add
        const fallbackTenant: Tenant = {
          id: `t-${Date.now()}`,
          name: this.newTenant.name,
          domain: this.newTenant.domain,
          contactEmail: this.newTenant.contactEmail,
          contactPhone: '',
          subscriptionPlanId: '',
          logoImageUrl: null,
          isDeleted: false,
          plan: this.newTenant.plan,
          status: 'Active',
          isActive: true,
          createdAt: new Date().toISOString(),
          usersCount: 1,
          projectsCount: 1,
          monthlyRevenue: this.newTenant.plan === 'Enterprise' ? 180 : (this.newTenant.plan === 'Pro' ? 45 : 15)
        };
        this.filteredTenants.unshift(fallbackTenant);
        this.totalCount++;
        this.closeCreateModal();
      }
    });
  }

  // VIEW DETAILS
  openViewModal(tenant: Tenant) {
    this.selectedTenant = tenant;
    this.isViewModalOpen = true;
  }

  closeViewModal() {
    this.selectedTenant = null;
    this.isViewModalOpen = false;
  }

  // EDIT
  openEditModal(tenant: Tenant) {
    this.editTenantForm = {
      id: tenant.id,
      name: tenant.name,
      plan: tenant.plan || 'Basic',
      contactEmail: tenant.contactEmail,
      domain: tenant.domain,
      status: tenant.status || 'Active'
    };
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
  }

  saveEditTenant() {
    if (!this.editTenantForm.name || !this.editTenantForm.id) return;

    this.tenantService.update(this.editTenantForm.id, this.editTenantForm).subscribe({
      next: () => {
        this.updateLocalTenant(this.editTenantForm);
        this.closeEditModal();
      },
      error: () => {
        // Optimistic local update
        this.updateLocalTenant(this.editTenantForm);
        this.closeEditModal();
      }
    });
  }

  private updateLocalTenant(form: any) {
    const idx = this.filteredTenants.findIndex(t => t.id === form.id);
    if (idx !== -1) {
      this.filteredTenants[idx] = {
        ...this.filteredTenants[idx],
        name: form.name,
        domain: form.domain,
        contactEmail: form.contactEmail,
        plan: form.plan,
        status: form.status,
        isActive: form.status === 'Active' || form.status === 'Upgraded'
      };
    }
  }

  // TOGGLE STATUS (Deactivate / Reactivate)
  toggleStatus(tenant: Tenant) {
    const updatedStatus = tenant.status === 'Suspended' ? 'Active' : 'Suspended';
    const payload = { ...tenant, status: updatedStatus, isActive: updatedStatus === 'Active' };
    
    this.tenantService.update(tenant.id, payload).subscribe({
      next: () => {
        tenant.status = updatedStatus;
        tenant.isActive = updatedStatus === 'Active';
      },
      error: () => {
        tenant.status = updatedStatus;
        tenant.isActive = updatedStatus === 'Active';
      }
    });
  }

  // DELETE
  deleteTenant(id: string) {
    if (confirm('Are you sure you want to delete this tenant organization? This action is permanent.')) {
      this.tenantService.delete(id).subscribe({
        next: () => {
          this.filteredTenants = this.filteredTenants.filter(t => t.id !== id);
          this.totalCount = Math.max(0, this.totalCount - 1);
        },
        error: () => {
          this.filteredTenants = this.filteredTenants.filter(t => t.id !== id);
          this.totalCount = Math.max(0, this.totalCount - 1);
        }
      });
    }
  }
}
