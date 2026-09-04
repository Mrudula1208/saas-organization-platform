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
  logs: SystemLog[] = [];
  filteredLogs: SystemLog[] = [];

  searchQuery = '';
  actionFilter = '';
  startDate = '';
  endDate = '';

  loading = false;
  errorMessage = '';

  constructor(private systemLogService: SystemLogService) {}

  ngOnInit() {
    this.loadLogs();
  }

  loadLogs() {
    this.loading = true;
    this.errorMessage = '';

    this.systemLogService.getLogs(this.actionFilter, this.startDate, this.endDate).subscribe({
      next: (data: SystemLog[]) => {
        this.logs = data;
        this.loading = false;
        this.applyFilters();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load system logs. Please try again later.';
        this.logs = [];
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    const query = this.searchQuery.toLowerCase();
    this.filteredLogs = this.logs.filter(log => {
      const matchesSearch = !query ||
                            log.description.toLowerCase().includes(query) ||
                            log.action.toLowerCase().includes(query);
      return matchesSearch;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.loadLogs();
  }

  shortId(id?: string | null): string {
    return id ? id.slice(0, 8) : '';
  }

  badgeClass(action: string): string {
    if (action === 'SYSTEM_ERROR') {
      return 'badge-danger';
    }
    if (action === 'LOGIN_SUCCESS') {
      return 'badge-success';
    }
    if (action === 'ACCOUNT_LOCKOUT') {
      return 'badge-warning';
    }
    return 'badge-info';
  }
}