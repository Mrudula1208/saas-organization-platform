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

  isAddModalOpen = false;
  isEditModalOpen = false;

  newUser = { fullName: '', email: '', password: '', role: 'Member', profileImageUrl: '' };
  editUserForm = { id: '', fullName: '', email: '', role: '', isActive: true };

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
                            u.email.toLowerCase().includes(this.searchQuery.toLowerCase());
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

    this.userService.createUser(this.newUser).subscribe({
      next: () => {
        this.loadUsers();
        this.closeAddModal();
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

    this.userService.updateUser(this.editUserForm.id, this.editUserForm).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadUsers();
          this.closeEditModal();
        }
      }
    });
  }

  // DELETE USER
  deleteUser(id: string) {
    if (confirm('Are you sure you want to remove this user from your team?')) {
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

