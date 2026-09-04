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
  isLoadingProfile = false;
  isSavingProfile = false;
  
  // Password form
  passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
  isChangingPassword = false;
  
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
    this.loadProfile();
    this.restoreSettings();
    this.loadWorkspace();
  }

  loadProfile() {
    // Seed the form from JWT claims while the real profile loads
    if (this.currentUser) {
      this.profileForm.fullName = this.currentUser.fullName || '';
      this.profileForm.email = this.currentUser.email || '';
    }

    this.isLoadingProfile = true;
    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.isLoadingProfile = false;
        if (profile) {
          this.profileForm.fullName = profile.fullName || '';
          this.profileForm.email = profile.email || '';
          this.profileForm.profileImageUrl = profile.profileImageUrl || '';
        }
      },
      error: (err) => {
        this.isLoadingProfile = false;
        this.errorMessage = err?.error?.message || 'Failed to load your profile. Please try again.';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  restoreSettings() {
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

    const fullName = this.profileForm.fullName.trim();
    if (!fullName) {
      this.errorMessage = 'Full name cannot be empty.';
      return;
    }

    this.isSavingProfile = true;
    this.userService.updateProfile(fullName, this.profileForm.profileImageUrl.trim()).subscribe({
      next: (res) => {
        this.isSavingProfile = false;
        if (res.success) {
          this.successMessage = res.message || 'Profile saved successfully!';
          this.profileForm.fullName = fullName;
          // Keep session claims in sync so the navbar shows the new name
          this.auth.updateCurrentUser({ fullName });
          this.currentUser = this.auth.currentUser();
        } else {
          this.errorMessage = res.message || 'Failed to save profile. Please try again.';
        }
        this.clearMessages();
      },
      error: (err) => {
        this.isSavingProfile = false;
        this.errorMessage = err?.error?.message || 'Failed to save profile. Please try again.';
        this.clearMessages();
      }
    });
  }

  changePassword() {
    this.successMessage = '';
    this.errorMessage = '';

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm;

    if (!currentPassword || !newPassword || !confirmPassword) {
      this.errorMessage = 'Please fill in all password fields.';
      return;
    }

    if (newPassword.length < 6) {
      this.errorMessage = 'New password must be at least 6 characters.';
      return;
    }

    if (newPassword !== confirmPassword) {
      this.errorMessage = 'New password and confirmation do not match.';
      return;
    }

    this.isChangingPassword = true;
    this.userService.changePassword(currentPassword, newPassword, confirmPassword).subscribe({
      next: (res) => {
        this.isChangingPassword = false;
        if (res.success) {
          this.successMessage = res.message || 'Password changed successfully!';
          this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        } else {
          this.errorMessage = res.message || 'Failed to change password. Please try again.';
        }
        this.clearMessages();
      },
      error: (err) => {
        this.isChangingPassword = false;
        this.errorMessage = err?.error?.message || 'Failed to change password. Please try again.';
        this.clearMessages();
      }
    });
  }

  private clearMessages(delayMs = 5000) {
    setTimeout(() => { this.successMessage = ''; this.errorMessage = ''; }, delayMs);
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

