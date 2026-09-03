import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth, UserClaims } from '../../../core/services/auth';
import { UserService } from '../../../core/services/user';
import { TenantService } from '../../../core/services/tenant';

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

  // Workspace logo state
  tenantName = '';
  tenantLogoUrl = '';
  logoPreviewUrl = '';
  selectedLogoFile: File | null = null;
  isUploadingLogo = false;
  logoUploadError = '';

  constructor(
    private auth: Auth,
    private userService: UserService,
    private tenantService: TenantService
  ) {}

  ngOnInit() {
    this.currentUser = this.auth.currentUser();
    this.restoreSettings();
    this.loadWorkspace();
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

    this.userService.updateProfile(this.profileForm.fullName, this.profileForm.profileImageUrl).subscribe({
      next: (success) => {
        if (success) {
          this.successMessage = 'Profile information saved successfully!';
          if (this.currentUser) {
            this.currentUser.fullName = this.profileForm.fullName;
          }
        } else {
          this.errorMessage = 'Failed to save profile. Please try again.';
        }
        setTimeout(() => { this.successMessage = ''; this.errorMessage = ''; }, 3000);
      },
      error: () => {
        this.errorMessage = 'Failed to save profile. Please try again.';
        setTimeout(() => this.errorMessage = '', 3000);
      }
    });
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

    this.userService.changePassword(this.passwordForm.currentPassword, this.passwordForm.newPassword).subscribe({
      next: (success) => {
        if (success) {
          this.successMessage = 'Password changed successfully!';
          this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        } else {
          this.errorMessage = 'Current password is incorrect. Please try again.';
        }
        setTimeout(() => { this.successMessage = ''; this.errorMessage = ''; }, 3000);
      },
      error: () => {
        this.errorMessage = 'Failed to change password. Please try again.';
        setTimeout(() => this.errorMessage = '', 3000);
      }
    });
  }

  savePreferences() {
    this.successMessage = '';
    
    if (typeof window !== 'undefined') {
      localStorage.setItem('tenant_prefs', JSON.stringify(this.preferencesForm));
    }
    
    this.successMessage = 'Notification preferences updated!';
    setTimeout(() => this.successMessage = '', 3000);
  }

  private loadWorkspace() {
    const tenantId = this.auth.getTenantId();
    if (!tenantId) return;

    this.tenantService.getById(tenantId).subscribe({
      next: (tenant) => {
        if (tenant) {
          this.tenantName = tenant.name;
          this.tenantLogoUrl = tenant.logoImageUrl || '';
        }
      }
    });
  }

  onLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] || null;
    this.logoUploadError = '';

    if (!file) return;

    const allowedTypes = ['image/png', 'image/jpeg', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      this.logoUploadError = 'Please choose a PNG, JPG, JPEG, WEBP or GIF image.';
      this.clearLogoSelection(input);
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.logoUploadError = 'Logo image must be 2 MB or smaller.';
      this.clearLogoSelection(input);
      return;
    }

    this.selectedLogoFile = file;
    if (typeof window !== 'undefined') {
      if (this.logoPreviewUrl) {
        URL.revokeObjectURL(this.logoPreviewUrl);
      }
      this.logoPreviewUrl = URL.createObjectURL(file);
    }
  }

  private clearLogoSelection(input: HTMLInputElement) {
    input.value = '';
    this.selectedLogoFile = null;
    this.logoPreviewUrl = '';
  }

  uploadLogo() {
    this.logoUploadError = '';
    this.errorMessage = '';

    if (!this.selectedLogoFile) {
      this.logoUploadError = 'Please choose a logo image first.';
      return;
    }

    const tenantId = this.auth.getTenantId();
    if (!tenantId) {
      this.logoUploadError = 'Unable to determine your workspace. Please sign in again.';
      return;
    }

    this.isUploadingLogo = true;

    this.tenantService.uploadLogo(tenantId, this.selectedLogoFile).subscribe({
      next: (logoUrl) => {
        this.isUploadingLogo = false;
        this.tenantLogoUrl = logoUrl;
        this.logoPreviewUrl = '';
        this.selectedLogoFile = null;
        this.successMessage = 'Workspace logo updated successfully!';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.isUploadingLogo = false;
        this.logoUploadError = err?.message || 'Failed to upload logo. Please try again.';
      }
    });
  }
}

