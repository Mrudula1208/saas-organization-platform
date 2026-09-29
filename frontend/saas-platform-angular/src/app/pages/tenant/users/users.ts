import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
import { User } from '../../../models/user.model';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class Users implements OnInit {
  filteredUsers: User[] = [];
  mostActiveUsers: User[] = [];

  tenantName = 'TechNova Solutions';

  searchQuery = '';
  roleFilter = '';
  errorMessage = '';
  successMessage = '';

  // Server-side pagination
  page = 1;
  pageSize = 6;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Selection
  selectAll = false;
  selectedUserIds = new Set<string>();

  // Modals
  isAddModalOpen = false;
  isEditModalOpen = false;
  isViewModalOpen = false;
  isDeleteModalOpen = false;

  // Password visibility
  showPassword = false;

  newUser = {
    fullName: '',
    email: '',
    password: '',
    role: 'Member',
    guestType: 'Member',
    profileImageUrl: ''
  };

  editUserForm = {
    id: '',
    fullName: '',
    email: '',
    role: 'Member',
    isActive: true,
    profileImageUrl: ''
  };

  viewingUser: User | null = null;
  deletingUser: User | null = null;

  addLoading = false;
  editLoading = false;
  deleteLoading = false;
  toggleLoadingId: string | null = null;

  activeMoreMenuId: string | null = null;

  constructor(
    private userService: UserService,
    public auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  get canManageUsers(): boolean {
    return this.auth.hasRole(['TenantAdmin', 'SuperAdmin']);
  }

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Admin';
  }

  ngOnInit() {
    if (typeof window !== 'undefined') {
      const storedTenant = localStorage.getItem('saas_tenant_name');
      if (storedTenant) {
        this.tenantName = storedTenant;
      }
    }
    this.loadUsers();
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.activeMoreMenuId = null;
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardShortcut(event: KeyboardEvent) {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      const input = document.getElementById('user-search-input') as HTMLInputElement;
      if (input) input.focus();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get pageStartItem(): number {
    return this.totalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageEndItem(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  get pagesArray(): number[] {
    const total = this.totalPages;
    const current = this.page;
    const delta = 2;
    const range: number[] = [];

    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  }

  loadUsers() {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.userService.getUsers(this.page, this.pageSize, this.searchQuery, this.roleFilter).subscribe({
      next: (res) => {
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadUsers();
          return;
        }
        this.filteredUsers = res.data;
        this.totalCount = res.totalCount;

        if (this.mostActiveUsers.length === 0 && res.data.length > 0) {
          this.mostActiveUsers = res.data.slice(0, 5);
        }

        this.loading = false;
        this.selectAll = false;
        this.selectedUserIds.clear();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load team members. Please try again later.');
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
    }, 350);
  }

  onFilterChange() {
    this.page = 1;
    this.loadUsers();
  }

  setPage(p: number) {
    if (p >= 1 && p <= this.totalPages && p !== this.page) {
      this.page = p;
      this.loadUsers();
    }
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

  // Row Selection
  toggleSelectAll() {
    this.selectAll = !this.selectAll;
    if (this.selectAll) {
      this.filteredUsers.forEach(u => this.selectedUserIds.add(u.id));
    } else {
      this.selectedUserIds.clear();
    }
  }

  toggleRowSelection(id: string, event: Event) {
    event.stopPropagation();
    if (this.selectedUserIds.has(id)) {
      this.selectedUserIds.delete(id);
      this.selectAll = false;
    } else {
      this.selectedUserIds.add(id);
      if (this.selectedUserIds.size === this.filteredUsers.length) {
        this.selectAll = true;
      }
    }
  }

  isRowSelected(id: string): boolean {
    return this.selectedUserIds.has(id);
  }

  // View User Modal
  openViewModal(user: User, event?: Event) {
    if (event) event.stopPropagation();
    this.viewingUser = user;
    this.isViewModalOpen = true;
    this.activeMoreMenuId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeViewModal() {
    this.isViewModalOpen = false;
    this.viewingUser = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  // ADD USER (CreateUser.tsx)
  openAddModal() {
    this.newUser = {
      fullName: '',
      email: '',
      password: '',
      role: 'Member',
      guestType: 'Member',
      profileImageUrl: ''
    };
    this.showPassword = false;
    this.errorMessage = '';
    this.isAddModalOpen = true;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeAddModal() {
    this.isAddModalOpen = false;
    this.errorMessage = '';
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveNewUser() {
    if (!this.newUser.fullName.trim() || !this.newUser.email.trim() || !this.newUser.password.trim()) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    this.addLoading = true;
    this.errorMessage = '';

    const payload = {
      fullName: this.newUser.fullName.trim(),
      email: this.newUser.email.trim(),
      password: this.newUser.password.trim(),
      role: this.newUser.role === 'Admin' ? 'TenantAdmin' : (this.newUser.role || 'Member'),
      profileImageUrl: this.newUser.profileImageUrl
    };

    this.userService.createUser(payload).subscribe({
      next: () => {
        this.addLoading = false;
        this.closeAddModal();
        this.loadUsers();
      },
      error: (err) => {
        this.addLoading = false;
        this.errorMessage = getErrorMessage(err, 'Could not create user. Verify email address is unique.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // EDIT USER
  openEditModal(user: User, event?: Event) {
    if (event) event.stopPropagation();
    this.editUserForm = {
      id: user.id,
      fullName: user.fullName,
      email: user.email,
      role: user.role === 'TenantAdmin' ? 'Admin' : (user.role || 'Member'),
      isActive: user.isActive,
      profileImageUrl: user.profileImageUrl || ''
    };
    this.isEditModalOpen = true;
    this.activeMoreMenuId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.errorMessage = '';
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveEditUser() {
    if (!this.editUserForm.fullName.trim() || !this.editUserForm.id) return;

    this.editLoading = true;
    this.errorMessage = '';

    const payload = {
      name: this.editUserForm.fullName.trim(),
      email: this.editUserForm.email.trim(),
      role: this.editUserForm.role === 'Admin' ? 'TenantAdmin' : this.editUserForm.role,
      isActive: this.editUserForm.isActive,
      profileImageUrl: this.editUserForm.profileImageUrl
    };

    this.userService.updateUser(this.editUserForm.id, payload).subscribe({
      next: (success: boolean) => {
        this.editLoading = false;
        if (success) {
          this.closeEditModal();
          this.loadUsers();
        }
      },
      error: (err) => {
        this.editLoading = false;
        this.errorMessage = getErrorMessage(err, 'Could not update user details.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // TOGGLE STATUS (Block / Activate)
  toggleUserStatus(user: User, event?: Event) {
    if (event) event.stopPropagation();
    this.toggleLoadingId = user.id;

    const newActiveState = !user.isActive;
    const payload = {
      name: user.fullName,
      email: user.email,
      role: user.role,
      isActive: newActiveState,
      profileImageUrl: user.profileImageUrl || ''
    };

    this.userService.updateUser(user.id, payload).subscribe({
      next: (success) => {
        this.toggleLoadingId = null;
        if (success) {
          user.isActive = newActiveState;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.toggleLoadingId = null;
        alert(getErrorMessage(err, 'Failed to update user status.'));
        this.cdr.markForCheck();
      }
    });
  }

  // DELETE USER
  openDeleteModal(user: User, event?: Event) {
    if (event) event.stopPropagation();
    this.deletingUser = user;
    this.isDeleteModalOpen = true;
    this.activeMoreMenuId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
    this.deletingUser = null;
    this.deleteLoading = false;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  confirmDeleteUser() {
    if (!this.deletingUser) return;

    this.deleteLoading = true;
    this.userService.deleteUser(this.deletingUser.id).subscribe({
      next: (success: boolean) => {
        this.deleteLoading = false;
        if (success) {
          this.closeDeleteModal();
          this.loadUsers();
        }
      },
      error: (err) => {
        this.deleteLoading = false;
        alert(getErrorMessage(err, 'Could not delete user.'));
        this.closeDeleteModal();
      }
    });
  }

  // More options
  toggleMoreMenu(userId: string, event: Event) {
    event.stopPropagation();
    this.activeMoreMenuId = this.activeMoreMenuId === userId ? null : userId;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  // Helpers
  getUserInitials(name: string): string {
    if (!name) return 'U';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  getUserAvatarColor(name: string): string {
    if (!name) return '#2563EB';
    const colors = ['#2563EB', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EC4899', '#6366F1'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }

  getRoleDisplayName(role: string): string {
    if (role === 'TenantAdmin' || role === 'Admin') return 'Admin';
    if (role === 'Manager') return 'Manager';
    if (role === 'Guest') return 'Guest';
    return 'Member';
  }

  getRoleBadgeClass(role: string): string {
    const r = (role || '').toLowerCase();
    if (r === 'tenantadmin' || r === 'admin') return 'role-admin';
    if (r === 'manager') return 'role-manager';
    if (r === 'guest') return 'role-guest';
    return 'role-member';
  }
}
