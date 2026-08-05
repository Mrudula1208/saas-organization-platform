import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth, UserClaims } from '../../../core/services/auth';
import { UserService } from '../../../core/services/user';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  currentUser: UserClaims | null = null;
  
  // Profile form
  profileForm = { fullName: '', email: '', profileImageUrl: '' };
  
  // Password form
  passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
  
  // Preferences form
  preferencesForm = { emailNotifications: true, inAppNotifications: true };
  
  isDarkTheme = true;
  successMessage = '';
  errorMessage = '';

  constructor(private auth: Auth, private userService: UserService) {}

  ngOnInit() {
    this.currentUser = this.auth.currentUser();
    this.restoreSettings();
  }

  restoreSettings() {
    // Restore profile
    if (this.currentUser) {
      this.profileForm.fullName = this.currentUser.fullName || 'User';
      this.profileForm.email = this.currentUser.email || '';
      
      // Attempt to load full user details for profile image
      this.userService.getUsers().subscribe({
        next: (users: any[]) => {
          const matched = users.find((u: any) => u.email.toLowerCase() === this.profileForm.email.toLowerCase());
          if (matched) {
            this.profileForm.profileImageUrl = matched.profileImageUrl || '';
          }
        }
      });
    }

    // Restore theme preference
    if (typeof window !== 'undefined') {
      const storedTheme = localStorage.getItem('theme_preference');
      this.isDarkTheme = storedTheme !== 'light';
      if (!this.isDarkTheme) {
        document.body.classList.add('light-theme');
      } else {
        document.body.classList.remove('light-theme');
      }

      const storedPrefs = localStorage.getItem('tenant_prefs');
      if (storedPrefs) {
        this.preferencesForm = JSON.parse(storedPrefs);
      }
    }
  }

  toggleTheme() {
    this.isDarkTheme = !this.isDarkTheme;
    if (typeof window !== 'undefined') {
      if (this.isDarkTheme) {
        document.body.classList.remove('light-theme');
        localStorage.setItem('theme_preference', 'dark');
      } else {
        document.body.classList.add('light-theme');
        localStorage.setItem('theme_preference', 'light');
      }
    }
  }

  saveProfile() {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.profileForm.fullName) {
      this.errorMessage = 'Profile name cannot be empty.';
      return;
    }

    // Simulate saving profile details
    this.successMessage = 'Profile information saved successfully!';
    setTimeout(() => this.successMessage = '', 3000);
  }

  changePassword() {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword || !this.passwordForm.confirmPassword) {
      this.errorMessage = 'Please fill in all password fields.';
      return;
    }

    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.errorMessage = 'New password and confirmation do not match.';
      return;
    }

    // Simulate password change
    this.successMessage = 'Password changed successfully!';
    this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
    setTimeout(() => this.successMessage = '', 3000);
  }

  savePreferences() {
    this.successMessage = '';
    
    if (typeof window !== 'undefined') {
      localStorage.setItem('tenant_prefs', JSON.stringify(this.preferencesForm));
    }
    
    this.successMessage = 'Notification preferences updated!';
    setTimeout(() => this.successMessage = '', 3000);
  }
}

