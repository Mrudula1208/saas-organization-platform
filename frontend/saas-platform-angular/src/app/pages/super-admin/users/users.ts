import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user';
import { TenantService } from '../../../core/services/tenant';
import { User } from '../../../models/user.model';
import { Tenant } from '../../../models/tenant.model';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class Users implements OnInit {
  filteredUsers: User[] = [];
  tenants: Tenant[] = [];

  // Filters matching Image: Search, All Tenants, All Roles, All Status
  searchQuery = '';
  tenantFilter = '';
  roleFilter = '';
  statusFilter = '';
  errorMessage = '';

  // Server-side pagination
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Add User Modal
  isCreateModalOpen = false;
  newUser = { fullName: '', email: '', password: 'Password123!', tenantId: '', role: 'Member' };
  savingUser = false;

  constructor(
    private userService: UserService,
    private tenantService: TenantService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadTenants();
    this.loadUsers();
  }

  loadTenants() {
    this.tenantService.getAll(1, 100).subscribe({
      next: (res) => {
        this.tenants = res.data || [];
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.tenants = [];
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
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

  loadUsers() {
    this.loading = true;
    this.errorMessage = '';
    const isActiveParam = this.statusFilter === 'active' ? true : (this.statusFilter === 'suspended' ? false : undefined);
    
    this.userService.getUsers(this.page, this.pageSize, this.searchQuery, this.roleFilter, isActiveParam).subscribe({
      next: (res) => {
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadUsers();
          return;
        }

        let result = res.data || [];
        if (this.tenantFilter) {
          result = result.filter(u => u.tenantId === this.tenantFilter || (u.tenant && u.tenant.id === this.tenantFilter));
        }

        this.filteredUsers = result;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load users. Please try again later.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  onSearch() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadUsers();
    }, 400);
  }

  onFilterChange() {
    this.page = 1;
    this.loadUsers();
  }

  resetFilters() {
    this.searchQuery = '';
    this.tenantFilter = '';
    this.roleFilter = '';
    this.statusFilter = '';
    this.page = 1;
    this.loadUsers();
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadUsers();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadUsers();
    }
  }

  goToPage(p: number) {
    if (p >= 1 && p <= this.totalPages) {
      this.page = p;
      this.loadUsers();
    }
  }

  toggleUserStatus(user: User) {
    const action = user.isActive ? 'suspend' : 'activate';
    if (!confirm(`Are you sure you want to ${action} access for ${user.fullName}?`)) {
      return;
    }
    this.errorMessage = '';
    this.userService.toggleStatus(user.id, !user.isActive).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the user status.');
      }
    });
  }

  deleteUser(id: string) {
    if (confirm('Are you sure you want to delete this user? This cannot be undone.')) {
      this.errorMessage = '';
      this.userService.deleteUser(id).subscribe({
        next: () => this.loadUsers(),
        error: (err) => {
          this.errorMessage = getErrorMessage(err, 'Could not delete the user.');
        }
      });
    }
  }

  resetPassword(user: User) {
    const newPass = prompt(`Reset password for ${user.fullName}:`, 'Password123!');
    if (newPass) {
      alert(`Temporary password for ${user.fullName} set to: ${newPass}`);
    }
  }

  exportUsers() {
    if (this.filteredUsers.length === 0) {
      alert('No users to export.');
      return;
    }

    const headers = ['User ID', 'Full Name', 'Email', 'Tenant', 'Role', 'Status', 'Created Date'];
    const rows = this.filteredUsers.map(u => [
      u.id,
      `"${u.fullName}"`,
      u.email,
      `"${u.tenant?.name || 'Platform'}"`,
      u.role,
      u.isActive ? 'Active' : 'Suspended',
      u.createdAt
    ]);

    const csvContent = 'data:text/csv;charset=utf-8,' + [headers.join(','), ...rows.map(e => e.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `users_export_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  // View User Modal (showing all required columns: User ID, User Name, Email, Tenant, Role, Status, Created Date, Last Login)
  selectedUserForView: User | null = null;

  // Edit User Modal
  selectedUserForEdit: { id: string; name: string; email: string; role: string; profileImageUrl: string; tenantId: string } | null = null;
  savingEdit = false;

  // Row Dropdown Menu tracking
  openDropdownUserId: string | null = null;

  openViewModal(user: User) {
    this.selectedUserForView = user;
    this.openDropdownUserId = null;
  }

  closeViewModal() {
    this.selectedUserForView = null;
  }

  openEditModal(user: User) {
    this.selectedUserForEdit = {
      id: user.id,
      name: user.fullName,
      email: user.email,
      role: user.role,
      profileImageUrl: user.profileImageUrl || '',
      tenantId: user.tenantId || user.tenant?.id || ''
    };
    this.openDropdownUserId = null;
  }

  closeEditModal() {
    this.selectedUserForEdit = null;
  }

  saveEditUser() {
    if (!this.selectedUserForEdit || !this.selectedUserForEdit.name) return;

    this.savingEdit = true;
    this.userService.updateUser(this.selectedUserForEdit.id, {
      name: this.selectedUserForEdit.name,
      email: this.selectedUserForEdit.email,
      role: this.selectedUserForEdit.role,
      profileImageUrl: this.selectedUserForEdit.profileImageUrl
    }).subscribe({
      next: () => {
        this.savingEdit = false;
        this.closeEditModal();
        this.loadUsers();
      },
      error: (err) => {
        this.savingEdit = false;
        this.errorMessage = getErrorMessage(err, 'Could not update user details.');
      }
    });
  }

  toggleDropdown(userId: string, event: Event) {
    event.stopPropagation();
    this.openDropdownUserId = this.openDropdownUserId === userId ? null : userId;
  }

  closeDropdown() {
    this.openDropdownUserId = null;
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

  openCreateModal() {
    this.newUser = {
      fullName: '',
      email: '',
      password: 'Password123!',
      tenantId: this.tenants[0]?.id || '',
      role: 'Member'
    };
    this.isCreateModalOpen = true;
    this.openDropdownUserId = null;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  saveNewUser() {
    if (!this.newUser.fullName || !this.newUser.email || !this.newUser.password) return;

    this.savingUser = true;
    this.userService.createUser(this.newUser).subscribe({
      next: () => {
        this.savingUser = false;
        this.closeCreateModal();
        this.loadUsers();
      },
      error: (err) => {
        this.savingUser = false;
        this.errorMessage = getErrorMessage(err, 'Could not create user.');
      }
    });
  }
}
