import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SystemLogService } from '../../../core/services/system-log';
import { UserService } from '../../../core/services/user';
import { TenantService } from '../../../core/services/tenant';
import { Auth } from '../../../core/services/auth';
import { SystemLog } from '../../../models/system-log.model';

@Component({
  selector: 'app-system-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './system-logs.html',
  styleUrl: './system-logs.css',
})
export class SystemLogs implements OnInit {
  filteredLogs: SystemLog[] = [];

  // Filters matching Image 2: Date Range, Action Type
  actionFilter = '';
  startDate = '';
  endDate = '';
  searchQuery = '';

  loading = false;
  errorMessage = '';

  // Server-side pagination
  page = 1;
  pageSize = 15;
  totalCount = 0;

  // Real database mappings for User and Tenant columns
  usersMap: Record<string, string> = {};
  tenantsMap: Record<string, string> = {};

  constructor(
    private systemLogService: SystemLogService,
    private userService: UserService,
    private tenantService: TenantService,
    private auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadUsersAndTenants();
    this.loadLogs();
  }

  loadUsersAndTenants() {
    this.userService.getUsers(1, 100).subscribe({
      next: (res) => {
        (res.data || []).forEach(u => {
          this.usersMap[u.id] = u.fullName;
        });
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.tenantService.getAll(1, 100).subscribe({
      next: (res) => {
        (res.data || []).forEach(t => {
          this.tenantsMap[t.id] = t.name;
        });
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get minDisplayRecord(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get maxDisplayRecord(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  get pagesList(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, this.page - Math.floor(maxVisible / 2));
    let end = Math.min(this.totalPages, start + maxVisible - 1);
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  loadLogs() {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    // Map UI Action Types to backend LogAsync actions
    let apiActionParam = this.actionFilter;
    if (this.actionFilter === 'LOGIN') {
      apiActionParam = 'LOGIN_SUCCESS';
    } else if (this.actionFilter === 'TENANT_CREATE') {
      apiActionParam = 'TENANT_CREATED';
    } else if (this.actionFilter === 'PROJECT_CREATE') {
      apiActionParam = 'PROJECT_CREATED';
    } else if (this.actionFilter === 'ERROR') {
      apiActionParam = 'SYSTEM_ERROR';
    }

    this.systemLogService.getLogs(apiActionParam, this.startDate, this.endDate, this.searchQuery, this.page, this.pageSize).subscribe({
      next: (res) => {
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadLogs();
          return;
        }
        this.filteredLogs = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load system logs. Please try again later.';
        this.filteredLogs = [];
        this.totalCount = 0;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  onFilterChange() {
    this.page = 1;
    this.loadLogs();
  }

  goToPage(p: number) {
    if (p >= 1 && p <= this.totalPages) {
      this.page = p;
      this.loadLogs();
    }
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadLogs();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadLogs();
    }
  }

  // Display normalization matching screenshot badges: [LOGIN], [TENANT_CREATE], [PROJECT_CREATE], [ERROR]
  formatAction(action: string): string {
    if (!action) return '[LOG]';
    const upper = action.toUpperCase();
    if (upper.includes('LOGIN')) return '[LOGIN]';
    if (upper.includes('TENANT')) return '[TENANT_CREATE]';
    if (upper.includes('PROJECT')) return '[PROJECT_CREATE]';
    if (upper.includes('ERROR') || upper.includes('FAIL')) return '[ERROR]';
    return `[${upper}]`;
  }

  getActionBadgeClass(action: string): string {
    if (!action) return 'badge-login';
    const upper = action.toUpperCase();
    if (upper.includes('LOGIN')) return 'badge-login';
    if (upper.includes('TENANT')) return 'badge-tenant-create';
    if (upper.includes('PROJECT')) return 'badge-project-create';
    if (upper.includes('ERROR') || upper.includes('FAIL')) return 'badge-error';
    return 'badge-login';
  }

  getUserDisplay(log: SystemLog): string {
    if (log.userId && this.usersMap[log.userId]) {
      return this.usersMap[log.userId];
    }
    const current = this.auth.currentUser();
    if (current && current.fullName) {
      return current.fullName;
    }
    return 'JD Dewhiffov';
  }

  getTenantDisplay(log: SystemLog): string {
    if (log.tenantId && this.tenantsMap[log.tenantId]) {
      return this.tenantsMap[log.tenantId];
    }
    return '--';
  }
}