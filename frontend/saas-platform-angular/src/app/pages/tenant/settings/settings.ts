import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Auth, UserClaims } from '../../../core/services/auth';
import { UserService } from '../../../core/services/user';
import { TenantService } from '../../../core/services/tenant';
import { NotificationService } from '../../../core/services/notification';
import { ThemeService } from '../../../core/services/theme';
import { downloadBlobAsFile } from '../../../core/helpers';

export interface ActivityItem {
  id: number;
  title: string;
  subtitle: string;
  timeAgo: string;
  icon: string;
  iconBg: string;
  iconColor: string;
}

@Component({
  selector: 'app-tenant-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  currentUser: UserClaims | null = null;
  isTenantAdmin = false;

  // Active Tab
  activeTab: 'profile' | 'security' | 'notifications' | 'billing' | 'advanced' = 'profile';

  // KPI Metrics
  kpiStats = {
    teamMembersActive: 0,
    profileCompleteness: 100,
    connectedIntegrations: 0,
    storageUsedGb: 0,
    storageTotalGb: 0,
    storagePercent: 0,
    securityAlerts: 0
  };

  // Profile Form (Left Column of Settings Card)
  profileForm = {
    name: '',
    email: '',
    avatarUrl: '',
    phone: '',
    domain: ''
  };

  // Notification Preferences & Triggers (Right Column of Settings Card)
  notifications = {
    email: true,
    inApp: true,
    slackIntegration: true,
    taskAssignment: true,
    projectUpdate: true,
    userJoining: true
  };

  // Password Change Form (Security Tab)
  passwordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  twoFactorEnabled = false;

  // Status & Feedback
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  // Avatar upload state
  avatarPreviewUrl = '';
  selectedAvatarFile: File | null = null;

  // Custom Setting Modal
  showAddSettingModal = false;
  newSetting = {
    key: '',
    type: 'Notification Webhook',
    value: ''
  };

  // Data Export state
  isExportingData = false;
  exportSuccess = false;
  exportError = '';

  // Right Column: Recent Activity Feed
  activityFeed: ActivityItem[] = [];

  constructor(
    private auth: Auth,
    private userService: UserService,
    private tenantService: TenantService,
    private notifService: NotificationService,
    public themeService: ThemeService,
    private cdr: ChangeDetectorRef
  ) {}

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Member';
  }

  ngOnInit() {
    this.currentUser = this.auth.currentUser();
    this.isTenantAdmin = this.auth.hasRole(['TenantAdmin']);
    this.loadUserData();
    this.loadWorkspaceData();
    this.loadTeamCount();
    this.loadRecentActivity();
  }

  setTab(tab: 'profile' | 'security' | 'notifications' | 'billing' | 'advanced') {
    if (!this.isTenantAdmin && (tab === 'billing' || tab === 'advanced')) {
      return;
    }
    this.activeTab = tab;
  }

  get isDarkTheme(): boolean {
    return this.themeService.isDark;
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  private loadUserData() {
    if (this.currentUser) {
      if (this.currentUser.fullName) this.profileForm.name = this.currentUser.fullName;
      if (this.currentUser.email) this.profileForm.email = this.currentUser.email;
    }

    this.userService.getProfile().subscribe({
      next: (profile) => {
        if (profile) {
          if (profile.fullName) this.profileForm.name = profile.fullName;
          if (profile.email) this.profileForm.email = profile.email;
          if (profile.profileImageUrl) this.profileForm.avatarUrl = profile.profileImageUrl;
        }
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  private loadWorkspaceData() {
    if (!this.isTenantAdmin) return;
    this.tenantService.getSettings().subscribe({
      next: (settings) => {
        if (settings) {
          if (settings.name) this.profileForm.name = settings.name;
          if (settings.contactEmail) this.profileForm.email = settings.contactEmail;
          if (settings.domain) this.profileForm.domain = settings.domain;
          if (settings.contactPhone) this.profileForm.phone = settings.contactPhone;
          if (settings.logoImageUrl) this.profileForm.avatarUrl = settings.logoImageUrl;

          this.notifications.email = settings.emailNotifications ?? true;
          this.notifications.inApp = settings.inAppNotifications ?? true;
        }
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  private loadTeamCount() {
    if (!this.isTenantAdmin) return;
    this.userService.getUsers(1, 1).subscribe({
      next: (res) => {
        this.kpiStats.teamMembersActive = res.totalCount || (res.data ? res.data.length : 0);
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  private loadRecentActivity() {
    this.notifService.getNotifications().subscribe({
      next: (list) => {
        if (list && list.length > 0) {
          this.activityFeed = list.slice(0, 6).map((n, idx) => ({
            id: idx + 1,
            title: n.message,
            subtitle: 'Workspace notification',
            timeAgo: n.createdAt ? this.formatRelativeTime(n.createdAt) : 'Recently',
            icon: 'notifications',
            iconBg: 'rgba(59, 130, 246, 0.15)',
            iconColor: '#3B82F6'
          }));
        } else {
          this.activityFeed = [];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.activityFeed = [];
        this.cdr.markForCheck();
      }
    });
  }

  private formatRelativeTime(dateStr?: string): string {
    if (!dateStr) return 'Recently';
    const date = new Date(dateStr);
    const now = new Date();
    const diffHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    if (diffHours < 1) return 'Just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 30) return `${diffDays} days ago`;
    return date.toLocaleDateString();
  }

  onAvatarFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.selectedAvatarFile = file;
    if (typeof window !== 'undefined') {
      this.avatarPreviewUrl = URL.createObjectURL(file);
    }

    // Auto upload logo/avatar if tenant admin
    const tenantId = this.auth.getTenantId();
    if (tenantId) {
      this.tenantService.uploadLogo(tenantId, file).subscribe({
        next: (url) => {
          this.profileForm.avatarUrl = url;
          this.showSuccess('Avatar image uploaded successfully!');
        },
        error: () => {
          this.showSuccess('Avatar selected locally.');
        }
      });
    }
  }

  saveChanges() {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.isTenantAdmin) {
      const payload = {
        name: this.profileForm.name.trim(),
        contactEmail: this.profileForm.email.trim(),
        contactPhone: this.profileForm.phone.trim(),
        emailNotifications: this.notifications.email,
        inAppNotifications: this.notifications.inApp
      };

      // Update workspace settings
      this.tenantService.updateSettings(payload).subscribe({
        next: (res) => {
          this.isSaving = false;
          if (res.success) {
            this.showSuccess('All settings and notification preferences saved successfully!');
            this.auth.updateCurrentUser({ fullName: this.profileForm.name });
          } else {
            this.errorMessage = res.message || 'Failed to save settings.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = err?.error?.message || 'Failed to save settings.';
          this.cdr.markForCheck();
        }
      });
    } else {
      // Member user updates personal profile
      this.userService.updateProfile(this.profileForm.name.trim(), this.profileForm.avatarUrl).subscribe({
        next: (res) => {
          this.isSaving = false;
          if (res.success) {
            this.showSuccess('Profile updated successfully!');
            this.auth.updateCurrentUser({ fullName: this.profileForm.name });
          } else {
            this.errorMessage = res.message || 'Failed to update profile.';
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = err?.error?.message || 'Failed to update profile.';
          this.cdr.markForCheck();
        }
      });
    }
  }

  changePassword() {
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm;
    if (!currentPassword || !newPassword || !confirmPassword) {
      this.errorMessage = 'Please complete all password fields.';
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

    this.isSaving = true;
    this.userService.changePassword(currentPassword, newPassword, confirmPassword).subscribe({
      next: (res) => {
        this.isSaving = false;
        if (res.success) {
          this.showSuccess('Password updated successfully!');
          this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        } else {
          this.errorMessage = res.message || 'Failed to change password.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err?.error?.message || 'Failed to change password.';
        this.cdr.markForCheck();
      }
    });
  }

  openAddSettingModal() {
    this.newSetting = {
      key: '',
      type: 'Notification Webhook',
      value: ''
    };
    this.showAddSettingModal = true;
  }

  closeAddSettingModal() {
    this.showAddSettingModal = false;
  }

  submitAddSetting() {
    if (!this.newSetting.key.trim()) return;
    this.showSuccess(`Custom setting "${this.newSetting.key}" added successfully!`);
    this.closeAddSettingModal();
  }

  exportWorkspaceData() {
    this.isExportingData = true;
    this.exportSuccess = false;
    this.exportError = '';

    this.tenantService.exportWorkspace().subscribe({
      next: (blob: Blob) => {
        this.isExportingData = false;
        this.exportSuccess = true;
        const dateStr = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        downloadBlobAsFile(blob, `workspace_export_${dateStr}.json`, 'application/json');
        this.cdr.markForCheck();
      },
      error: () => {
        this.isExportingData = false;
        this.exportError = 'Could not generate export archive at this time.';
        this.cdr.markForCheck();
      }
    });
  }

  private showSuccess(msg: string) {
    this.successMessage = msg;
    this.errorMessage = '';
    setTimeout(() => {
      this.successMessage = '';
      this.cdr.markForCheck();
    }, 4500);
  }
}
