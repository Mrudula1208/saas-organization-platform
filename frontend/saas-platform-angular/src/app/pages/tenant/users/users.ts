import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
import { User } from '../../../models/user.model';
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

  searchQuery = '';
  roleFilter = '';
  errorMessage = '';

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  isAddModalOpen = false;
  isEditModalOpen = false;

  newUser = { fullName: '', email: '', password: '', role: 'Member', profileImageUrl: '' };
  editUserForm = { id: '', fullName: '', email: '', role: '', isActive: true };

  constructor(
    private userService: UserService,
    public auth: Auth
  ) {}

  get canManageUsers(): boolean {
    return this.auth.hasRole(['TenantAdmin', 'SuperAdmin']);
  }

  ngOnInit() {
    this.loadUsers();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  loadUsers() {
    this.loading = true;
    this.errorMessage = '';
    this.userService.getUsers(this.page, this.pageSize, this.searchQuery, this.roleFilter).subscribe({
      next: (res) => {
        // The current page disappeared (e.g. last row deleted): show the last page that still has rows.
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadUsers();
          return;
        }
        this.filteredUsers = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load team members. Please try again later.');
      }
    });
  }

  onSearch() {
    // Ask the server only after the user stops typing.
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

  // ADD USER
  openAddModal() {
    this.newUser = { fullName: '', email: '', password: '', role: 'Member', profileImageUrl: '' };
    this.isAddModalOpen = true;
  }

  closeAddModal() {
    this.isAddModalOpen = false;
  }

  saveNewUser() {
    if (!this.newUser.fullName || !this.newUser.email || !this.newUser.password) return;

    this.errorMessage = '';
    this.userService.createUser(this.newUser).subscribe({
      next: () => {
        this.loadUsers();
        this.closeAddModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not create the user.');
      }
    });
  }

  // EDIT USER
  openEditModal(user: User) {
    this.editUserForm = {
      id: user.id,
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      isActive: user.isActive
    };
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
  }

  saveEditUser() {
    if (!this.editUserForm.fullName || !this.editUserForm.id) return;

    this.errorMessage = '';
    this.userService.updateUser(this.editUserForm.id, this.editUserForm).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadUsers();
          this.closeEditModal();
        }
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the user.');
      }
    });
  }

  // DELETE USER
  deleteUser(id: string) {
    if (confirm('Are you sure you want to remove this user from your team?')) {
      this.errorMessage = '';
      this.userService.deleteUser(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadUsers();
          }
        },
        error: (err) => {
          this.errorMessage = getErrorMessage(err, 'Could not delete the user.');
        }
      });
    }
  }
}
