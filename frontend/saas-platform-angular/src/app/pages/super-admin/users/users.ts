import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService, User } from '../../../core/services/user';

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

  constructor(private userService: UserService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.userService.getUsers().subscribe({
      next: (data: User[]) => {
        this.users = data;
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    this.filteredUsers = this.users.filter((u: User) => {
      const matchesSearch = u.fullName.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            u.email.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            (u.tenantName && u.tenantName.toLowerCase().includes(this.searchQuery.toLowerCase()));
      
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
    const updatedStatus = { isActive: !user.isActive };
    this.userService.updateUser(user.id, updatedStatus).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadUsers();
        }
      }
    });
  }

  deleteUser(id: string) {
    if (confirm('Are you sure you want to delete this user?')) {
      this.userService.deleteUser(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadUsers();
          }
        }
      });
    }
  }
}

