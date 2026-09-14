import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SystemLogService } from '../../../core/services/system-log';
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

  searchQuery = '';
  actionFilter = '';
  startDate = '';
  endDate = '';

  loading = false;
  errorMessage = '';

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(private systemLogService: SystemLogService) {}

  ngOnInit() {
    this.loadLogs();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  loadLogs() {
    this.loading = true;
    this.errorMessage = '';

    this.systemLogService.getLogs(this.actionFilter, this.startDate, this.endDate, this.searchQuery, this.page, this.pageSize).subscribe({
      next: (res) => {
        // The current page disappeared: show the last page that still has rows.
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadLogs();
          return;
        }
        this.filteredLogs = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load system logs. Please try again later.';
        this.filteredLogs = [];
        this.totalCount = 0;
      }
    });
  }

  onSearch() {
    // Ask the server only after the user stops typing.
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadLogs();
    }, 400);
  }

  onFilterChange() {
    this.page = 1;
    this.loadLogs();
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

  shortId(id?: string | null): string {
    return id ? id.slice(0, 8) : '';
  }

  badgeClass(action: string): string {
    if (action === 'SYSTEM_ERROR' || action === 'LOGIN_FAILED') {
      return 'badge-danger';
    }
    if (action === 'LOGIN_SUCCESS') {
      return 'badge-success';
    }
    if (action === 'ACCOUNT_LOCKOUT' || action.endsWith('_DELETED')) {
      return 'badge-warning';
    }
    return 'badge-info';
  }
}