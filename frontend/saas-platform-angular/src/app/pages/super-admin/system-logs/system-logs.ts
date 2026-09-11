import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface SystemLog {
  id: string;
  timestamp: string;
  type: 'Info' | 'Warning' | 'Error';
  source: string; // e.g. AuthController, TenantService
  message: string;
  userId?: string;
  userName?: string;
}

@Component({
  selector: 'app-system-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './system-logs.html',
  styleUrl: './system-logs.css',
})
export class SystemLogs implements OnInit {
  logs: SystemLog[] = [
    { id: 'LOG-001', timestamp: '2026-05-27T12:05:12Z', type: 'Info', source: 'AuthController', message: 'User admin@saas.com successfully logged in from IP 192.168.1.45', userId: 'user-1', userName: 'JD Dewifrav' },
    { id: 'LOG-002', timestamp: '2026-05-27T11:42:01Z', type: 'Info', source: 'TenantController', message: 'New organization tenant Initech Inc created successfully', userId: 'user-1', userName: 'JD Dewifrav' },
    { id: 'LOG-003', timestamp: '2026-05-27T10:15:30Z', type: 'Error', source: 'UserService', message: 'ArgumentNullException: Value cannot be null (Parameter Role) during login generation for user root@acme.com', userId: 'user-4', userName: 'Sarah Johnson' },
    { id: 'LOG-004', timestamp: '2026-05-27T09:30:15Z', type: 'Warning', source: 'ProjectService', message: 'Tenant Acme Corp has reached 90% of active projects quota limits', userId: 'user-2', userName: 'Emma Smith' },
    { id: 'LOG-005', timestamp: '2026-05-26T18:12:44Z', type: 'Info', source: 'TasksController', message: 'Task ID task-128 marked complete by assigned user member@acme.com', userId: 'user-3', userName: 'Michael Brown' },
    { id: 'LOG-006', timestamp: '2026-05-26T14:22:10Z', type: 'Info', source: 'TenantController', message: 'Tenant organization Acme Corp upgraded subscription plan to Pro Tier', userId: 'user-8', userName: 'Amy Lee' },
    { id: 'LOG-007', timestamp: '2026-05-26T11:05:00Z', type: 'Error', source: 'UserRepository', message: 'DbUpdateConcurrencyException: Entity Framework tracking error resolved for User update sequence' }
  ];

  filteredLogs: SystemLog[] = [];
  searchQuery = '';
  typeFilter = '';

  ngOnInit() {
    this.applyFilters();
  }

  applyFilters() {
    this.filteredLogs = this.logs.filter(log => {
      const matchesSearch = log.message.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            log.source.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            (log.userName && log.userName.toLowerCase().includes(this.searchQuery.toLowerCase()));
      
      const matchesType = this.typeFilter === '' || log.type === this.typeFilter;

      return matchesSearch && matchesType;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
  }

  clearLogs() {
    if (confirm('Are you sure you want to clear system diagnostic logs? This is irreversible.')) {
      this.logs = [];
      this.applyFilters();
    }
  }
}

