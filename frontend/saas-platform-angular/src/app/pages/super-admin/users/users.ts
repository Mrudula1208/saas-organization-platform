import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user';
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

  constructor(private userService: UserService) {}

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
        this.errorMessage = getErrorMessage(err, 'Could not load users. Please try again later.');
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

  toggleUserStatus(user: User) {
    this.errorMessage = '';
    this.userService.toggleStatus(user.id, !user.isActive).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the user status.');
      }
    });
  }

  deleteUser(id: string) {
    if (confirm('Are you sure you want to delete this user?')) {
      this.errorMessage = '';
      this.userService.deleteUser(id).subscribe({
        next: () => this.loadUsers(),
        error: (err) => {
          this.errorMessage = getErrorMessage(err, 'Could not delete the user.');
        }
      });
    }
  }
}
