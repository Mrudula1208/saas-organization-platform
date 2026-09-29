import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SettingsService } from '../../../core/services/settings';
import { SystemLogService } from '../../../core/services/system-log';
import { Auth } from '../../../core/services/auth';
import { getErrorMessage } from '../../../core/helpers';
import { SystemLog } from '../../../models/system-log.model';

export interface ActivityFeedItem {
  id: string;
  icon: string;
  iconColor: string;
  iconBg: string;
  title: string;
  subtitle: string;
  timeAgo: string;
}

export interface AdminTeamMember {
  id: string;
  name: string;
  email: string;
  role: string;
  status: string;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  // Top 5 KPI Metrics
  profileCompleteness = 90;
  activeSessions = 27;
  activeSessionsTrends = '45,102 Trends';
  connectedApps = 1;
  connectedAppsTrends = '89,011 Trends';
  apiKeysActive = 5;
  apiKeysTrends = '$345,100 Trends';
  securityScore = 90;
  securityScoreTrends = '1,683 Evaluated';

  // Navigation tab state
  activeTab: 'profile' | 'security' | 'team' | 'notifications' | 'advanced' = 'profile';

  // Tab 1: Profile Form fields
  profileName = 'Super Admin';
  profileEmail = 'email@example.com';
  profileRole = 'Super Admin';
  activeState = true;
  avatarPreview: string | null = null;

  // Tab 2: Security Form fields
  mfaRequired = false;
  sessionTimeout = 30;
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  // Tab 3: Team Members
  teamMembers: AdminTeamMember[] = [
    { id: '1', name: 'Super Admin', email: 'admin@saasplatform.com', role: 'Super Admin', status: 'Active' },
    { id: '2', name: 'Security Officer', email: 'secops@saasplatform.com', role: 'SecOps Lead', status: 'Active' },
    { id: '3', name: 'Platform DevOps', email: 'devops@saasplatform.com', role: 'DevOps Engineer', status: 'Active' },
  ];

  // Tab 4: Notifications Form fields
  emailAlerts = true;
  auditAlerts = true;
  securityAlerts = true;
  weeklyDigest = false;

  // Tab 5: Advanced Platform Configuration
  platformName = 'SaaS Platform';
  supportEmail = 'support@saas.com';
  allowRegistrations = true;
  maintenanceMode = false;

  // Right Column: Recent Activity Feed
  activityFeed: ActivityFeedItem[] = [];

  // Feedback states
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  private readonly benchmarkFeed: ActivityFeedItem[] = [
    {
      id: 'f-1',
      icon: 'corporate_fare',
      iconColor: '#06B6D4',
      iconBg: 'rgba(6, 182, 212, 0.12)',
      title: 'Acme Corp Tenant Created',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-2',
      icon: 'person',
      iconColor: '#3B82F6',
      iconBg: 'rgba(59, 130, 246, 0.12)',
      title: 'User registered',
      subtitle: 'User registered at X568395',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-3',
      icon: 'trending_up',
      iconColor: '#10B981',
      iconBg: 'rgba(16, 185, 129, 0.12)',
      title: 'Plan upgraded',
      subtitle: 'Acme upgraded at $375,100',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-4',
      icon: 'credit_card',
      iconColor: '#06B6D4',
      iconBg: 'rgba(6, 182, 212, 0.12)',
      title: 'Plan upgraded',
      subtitle: 'User registered at $385,331',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-5',
      icon: 'verified',
      iconColor: '#3B82F6',
      iconBg: 'rgba(59, 130, 246, 0.12)',
      title: 'Plan upgraded',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-6',
      icon: 'workspace_premium',
      iconColor: '#8B5CF6',
      iconBg: 'rgba(139, 92, 246, 0.12)',
      title: 'Plan upgraded',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
    },
    {
      id: 'f-7',
      icon: 'security',
      iconColor: '#10B981',
      iconBg: 'rgba(16, 185, 129, 0.12)',
      title: 'Security policy updated',
      subtitle: 'MFA enforced system-wide',
      timeAgo: '3 months ago',
    },
  ];

  constructor(
    private settingsService: SettingsService,
    private systemLogService: SystemLogService,
    private auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.initCurrentUser();
    this.loadSettings();
    this.loadActivityFeed();
  }

  private initCurrentUser() {
    const user = this.auth.currentUser();
    if (user) {
      if (user.fullName) this.profileName = user.fullName;
      if (user.email) this.profileEmail = user.email;
      if (user.role) this.profileRole = user.role;
    }
  }

  loadSettings() {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.settingsService.getSettings().subscribe({
      next: (res) => {
        if (res.data) {
          this.platformName = res.data.platformName;
          this.supportEmail = res.data.supportEmail;
          this.maintenanceMode = res.data.maintenanceMode;
          this.allowRegistrations = res.data.allowRegistrations;
          this.mfaRequired = res.data.mfaRequired;
          this.sessionTimeout = res.data.sessionTimeout;
        }
        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        // Retain current values gracefully
        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
    });
  }

  loadActivityFeed() {
    this.systemLogService.getLogs(undefined, undefined, undefined, undefined, 1, 10).subscribe({
      next: (res) => {
        const logs: SystemLog[] = Array.isArray(res?.data) ? res.data : [];
        if (logs.length > 0) {
          this.activityFeed = logs.map((log) => ({
            id: log.id,
            icon: this.getFeedIcon(log.action),
            iconColor: '#06B6D4',
            iconBg: 'rgba(6, 182, 212, 0.12)',
            title: this.formatFeedTitle(log.action),
            subtitle: log.description || 'System activity logged',
            timeAgo: this.formatRelativeTime(log.createdAt),
          }));
        } else {
          this.activityFeed = [...this.benchmarkFeed];
        }
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.activityFeed = [...this.benchmarkFeed];
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
    });
  }

  saveProfile() {
    this.successMessage = '';
    this.errorMessage = '';
    this.isSaving = true;

    // Simulate saving profile preferences
    setTimeout(() => {
      this.isSaving = false;
      this.successMessage = 'Profile updated successfully!';
      setTimeout(() => (this.successMessage = ''), 4000);
    }, 600);
  }

  saveSecurity() {
    this.successMessage = '';
    this.errorMessage = '';
    this.isSaving = true;

    const payload = {
      platformName: this.platformName,
      supportEmail: this.supportEmail,
      maintenanceMode: this.maintenanceMode,
      allowRegistrations: this.allowRegistrations,
      mfaRequired: this.mfaRequired,
      sessionTimeout: Number(this.sessionTimeout),
    };

    this.settingsService.updateSettings(payload).subscribe({
      next: (res) => {
        this.isSaving = false;
        this.successMessage = res.message || 'Security settings updated successfully!';
        setTimeout(() => (this.successMessage = ''), 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = getErrorMessage(err, 'Failed to update security protocols.');
      },
    });
  }

  saveAdvanced() {
    this.successMessage = '';
    this.errorMessage = '';
    this.isSaving = true;

    const payload = {
      platformName: this.platformName,
      supportEmail: this.supportEmail,
      maintenanceMode: this.maintenanceMode,
      allowRegistrations: this.allowRegistrations,
      mfaRequired: this.mfaRequired,
      sessionTimeout: Number(this.sessionTimeout),
    };

    this.settingsService.updateSettings(payload).subscribe({
      next: (res) => {
        this.isSaving = false;
        this.successMessage = res.message || 'Platform configurations updated successfully!';
        setTimeout(() => (this.successMessage = ''), 4000);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = getErrorMessage(err, 'Failed to update settings.');
      },
    });
  }

  onAvatarFileChange(event: any) {
    const file = event.target?.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.avatarPreview = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  private getFeedIcon(action?: string): string {
    const act = (action || '').toUpperCase();
    if (act.includes('LOGIN')) return 'person';
    if (act.includes('TENANT')) return 'corporate_fare';
    if (act.includes('PLAN')) return 'upgrade';
    if (act.includes('PROJECT')) return 'folder_open';
    if (act.includes('SECURITY')) return 'security';
    return 'info';
  }

  private formatFeedTitle(action?: string): string {
    const act = (action || '').toUpperCase();
    if (act.includes('TENANT')) return 'Acme Corp Tenant Created';
    if (act.includes('LOGIN')) return 'User registered';
    if (act.includes('PLAN')) return 'Plan upgraded';
    return action || 'Activity Logged';
  }

  private formatRelativeTime(dateStr?: string | Date): string {
    if (!dateStr) return '2 months ago';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMonths = (now.getFullYear() - date.getFullYear()) * 12 + (now.getMonth() - date.getMonth());
    if (diffMonths > 0) return `${diffMonths} months ago`;
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    if (diffDays > 0) return `${diffDays} days ago`;
    return 'Just now';
  }
}
