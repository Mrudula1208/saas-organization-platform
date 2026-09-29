import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { NotificationService } from '../../../core/services/notification';
import { ThemeService } from '../../../core/services/theme';
import { AppNotification } from '../../../models/notification.model';

export interface CommandItem {
  id: string;
  title: string;
  subtitle?: string;
  category: 'Navigation' | 'Actions';
  icon: string;
  badge?: string;
  action: () => void;
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  showNotifications = false;
  showProfile = false;

  // Command Palette State
  showCommandPalette = false;
  commandSearch = '';
  selectedCommandIndex = 0;

  constructor(
    private auth: Auth,
    private router: Router,
    public notifService: NotificationService,
    public themeService: ThemeService
  ) {}

  ngOnInit() {
    if (this.auth.isLoggedIn()) {
      this.notifService.loadUnreadCount();
      this.notifService.loadNotifications();
    }
  }

  get isDarkTheme(): boolean {
    return this.themeService.isDark;
  }

  get user() {
    return this.auth.currentUser;
  }

  get unreadCount() {
    return this.notifService.unreadCount;
  }

  get notifications() {
    return this.notifService.notifications();
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
    this.showProfile = false;
    if (this.showNotifications) {
      this.notifService.loadNotifications();
    }
  }

  toggleProfile() {
    this.showProfile = !this.showProfile;
    this.showNotifications = false;
  }

  markAllAsRead() {
    this.notifService.markAllRead().subscribe();
  }

  readNotification(notif: AppNotification) {
    if (!notif.isRead) {
      this.notifService.markRead(notif.id).subscribe();
    }
    this.showNotifications = false;
    this.router.navigate([this.getNotificationsLink()]);
  }

  getNotificationsLink(): string {
    const role = this.user()?.role;
    if (role === 'SuperAdmin') {
      return '/admin/system-logs';
    }
    return '/tenant/notifications';
  }

  getRoleLabel(role?: string): string {
    if (!role) return '';
    if (role === 'SuperAdmin') return 'Super Admin';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    if (role === 'Manager') return 'Manager';
    if (role === 'Member') return 'Member';
    return role;
  }

  getInitials(): string {
    const name = this.user()?.fullName || '';
    if (!name) return 'U';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
  }

  getNotifIcon(notif: AppNotification): string {
    if (!notif.isRead) return 'notifications_active';
    return 'notifications';
  }

  toggleSidebar() {
    if (typeof window !== 'undefined') {
      document.body.classList.toggle('sidebar-open');
    }
  }

  navigateToSettings() {
    this.showProfile = false;
    const role = this.user()?.role;
    if (role === 'SuperAdmin') {
      this.router.navigate(['/admin/settings']);
    } else {
      this.router.navigate(['/tenant/settings']);
    }
  }

  onLogout() {
    this.showProfile = false;
    const user = this.user();
    if (user && user.role !== 'SuperAdmin') {
      this.router.navigate(['/tenant/logout']);
    } else {
      this.router.navigate(['/logout']);
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays}d ago`;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.profile-menu') && !target.closest('.nav-btn')) {
      this.showProfile = false;
      this.showNotifications = false;
    }
  }

  // --- Command Palette Methods ---
  openCommandPalette() {
    this.showCommandPalette = true;
    this.commandSearch = '';
    this.selectedCommandIndex = 0;
    this.showNotifications = false;
    this.showProfile = false;
  }

  closeCommandPalette() {
    this.showCommandPalette = false;
    this.commandSearch = '';
  }

  toggleCommandPalette() {
    if (this.showCommandPalette) {
      this.closeCommandPalette();
    } else {
      this.openCommandPalette();
    }
  }

  getAllCommands(): CommandItem[] {
    const role = this.user()?.role;
    const isSuperAdmin = role === 'SuperAdmin';
    const isTenantAdmin = role === 'TenantAdmin';

    const commands: CommandItem[] = [];

    if (isSuperAdmin) {
      commands.push(
        { id: 'admin-dash', title: 'Global Dashboard', subtitle: 'Platform KPIs & Active Subscriptions', category: 'Navigation', icon: 'dashboard', badge: 'Admin', action: () => this.navigateAndClose('/admin/dashboard') },
        { id: 'admin-tenants', title: 'Tenants Directory', subtitle: 'Manage organization workspaces', category: 'Navigation', icon: 'corporate_fare', badge: 'Admin', action: () => this.navigateAndClose('/admin/tenants') },
        { id: 'admin-users', title: 'Global Users Directory', subtitle: 'Platform-wide user accounts', category: 'Navigation', icon: 'group', badge: 'Admin', action: () => this.navigateAndClose('/admin/users') },
        { id: 'admin-plans', title: 'Subscription Plans', subtitle: 'Tier quotas & pricing limits', category: 'Navigation', icon: 'loyalty', badge: 'Admin', action: () => this.navigateAndClose('/admin/subscription-plans') },
        { id: 'admin-rev', title: 'Revenue Analytics', subtitle: 'MRR, ARPU & billing receipts', category: 'Navigation', icon: 'analytics', badge: 'Admin', action: () => this.navigateAndClose('/admin/revenue') },
        { id: 'admin-logs', title: 'System Diagnostic Logs', subtitle: 'Audit traces & exception telemetry', category: 'Navigation', icon: 'terminal', badge: 'Admin', action: () => this.navigateAndClose('/admin/system-logs') },
        { id: 'admin-reports', title: 'Platform Reports Hub', subtitle: 'Consolidated ecosystem metrics', category: 'Navigation', icon: 'monitoring', badge: 'Admin', action: () => this.navigateAndClose('/admin/reports') },
        { id: 'admin-settings', title: 'System Settings', subtitle: 'Global configurations & security', category: 'Navigation', icon: 'settings', badge: 'Admin', action: () => this.navigateAndClose('/admin/settings') }
      );
    } else {
      commands.push(
        { id: 't-dash', title: 'Workspace Dashboard', subtitle: 'Project velocity & team overview', category: 'Navigation', icon: 'dashboard', action: () => this.navigateAndClose('/tenant/dashboard') },
        { id: 't-proj', title: 'Projects Workspace', subtitle: 'Deliverables & timeline tracking', category: 'Navigation', icon: 'folder_open', action: () => this.navigateAndClose('/tenant/projects') },
        { id: 't-tasks', title: 'Tasks Kanban Board', subtitle: 'Interactive drag-and-drop task workflow', category: 'Navigation', icon: 'assignment', badge: 'Kanban', action: () => this.navigateAndClose('/tenant/tasks') },
        { id: 't-members', title: 'Project Members', subtitle: 'Project team directory', category: 'Navigation', icon: 'badge', action: () => this.navigateAndClose('/tenant/project-members') }
      );

      if (isTenantAdmin) {
        commands.push(
          { id: 't-users', title: 'Team Members Directory', subtitle: 'Manage organization user accounts', category: 'Navigation', icon: 'group', action: () => this.navigateAndClose('/tenant/users') },
          { id: 't-reports', title: 'Analytics & Reports', subtitle: 'Productivity metrics & PDF exports', category: 'Navigation', icon: 'monitoring', action: () => this.navigateAndClose('/tenant/reports') },
          { id: 't-billing', title: 'Subscription & Billing', subtitle: 'Tier quota guardrails & invoices', category: 'Navigation', icon: 'receipt_long', badge: 'Billing', action: () => this.navigateAndClose('/tenant/billing') }
        );
      }

      commands.push(
        { id: 't-notif', title: 'Notifications Center', subtitle: 'View task assignments & system alerts', category: 'Navigation', icon: 'notifications', action: () => this.navigateAndClose('/tenant/notifications') },
        { id: 't-settings', title: 'Settings', subtitle: 'Account preferences & profile', category: 'Navigation', icon: 'settings', action: () => this.navigateAndClose('/tenant/settings') }
      );
    }

    // Global Actions
    commands.push(
      { id: 'act-theme', title: `Toggle ${this.isDarkTheme ? 'Light' : 'Dark'} Theme`, subtitle: 'Switch color theme preference', category: 'Actions', icon: 'contrast', action: () => { this.toggleTheme(); this.closeCommandPalette(); } },
      { id: 'act-notif', title: 'View All Notifications', subtitle: 'Open in-app activity notifications', category: 'Actions', icon: 'notifications', action: () => this.navigateAndClose(this.getNotificationsLink()) },
      { id: 'act-logout', title: 'Sign Out Session', subtitle: 'Safely end active user authentication', category: 'Actions', icon: 'logout', action: () => { this.closeCommandPalette(); this.onLogout(); } }
    );

    return commands;
  }

  get filteredCommands(): CommandItem[] {
    const list = this.getAllCommands();
    const query = this.commandSearch.trim().toLowerCase();
    if (!query) return list;
    return list.filter(c => 
      c.title.toLowerCase().includes(query) || 
      (c.subtitle && c.subtitle.toLowerCase().includes(query)) ||
      c.category.toLowerCase().includes(query)
    );
  }

  navigateAndClose(path: string) {
    this.closeCommandPalette();
    this.router.navigate([path]);
  }

  navigateCommands(direction: number) {
    const total = this.filteredCommands.length;
    if (total === 0) return;
    this.selectedCommandIndex = (this.selectedCommandIndex + direction + total) % total;
  }

  executeSelectedCommand() {
    const commands = this.filteredCommands;
    if (commands.length > 0 && this.selectedCommandIndex < commands.length) {
      commands[this.selectedCommandIndex].action();
    }
  }

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.toggleCommandPalette();
    } else if (event.key === 'Escape' && this.showCommandPalette) {
      this.closeCommandPalette();
    } else if (this.showCommandPalette) {
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        this.navigateCommands(1);
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        this.navigateCommands(-1);
      } else if (event.key === 'Enter') {
        event.preventDefault();
        this.executeSelectedCommand();
      }
    }
  }
}
