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
  users: User[] = [];
  filteredUsers: User[] = [];

  searchQuery = '';
  roleFilter = '';
  errorMessage = '';

  constructor(private userService: UserService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.errorMessage = '';
    this.userService.getUsers().subscribe({
      next: (data: User[]) => {
        this.users = data;
        this.applyFilters();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not load users. Please try again later.');
      }
    });
  }

  applyFilters() {
    this.filteredUsers = this.users.filter((u: User) => {
      const tenantName = u.tenant?.name || u.tenantName || '';
      const matchesSearch = u.fullName.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            u.email.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            tenantName.toLowerCase().includes(this.searchQuery.toLowerCase());

      const matchesRole = this.roleFilter === '' || u.role === this.roleFilter;

      return matchesSearch && matchesRole;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
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
