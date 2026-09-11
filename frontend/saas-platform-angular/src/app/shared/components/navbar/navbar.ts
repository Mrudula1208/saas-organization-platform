import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { NotificationService, AppNotification } from '../../../core/services/notification';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  isDarkTheme = true;
  showNotifications = false;
  showProfile = false;

  constructor(
    private auth: Auth,
    private router: Router,
    public notifService: NotificationService
  ) {
    this.detectSystemTheme();
  }

  ngOnInit() {
    if (this.auth.isLoggedIn()) {
      this.notifService.loadUnreadCount();
      this.notifService.loadNotifications();
    }
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

  detectSystemTheme() {
    if (typeof window !== 'undefined') {
      const isLightTheme = document.body.classList.contains('light-theme');
      this.isDarkTheme = !isLightTheme;
    }
  }

  toggleTheme() {
    if (typeof window !== 'undefined') {
      this.isDarkTheme = !this.isDarkTheme;
      if (this.isDarkTheme) {
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      } else {
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      }
    }
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
    if (role === 'Admin') {
      return '/admin/system-logs';
    }
    return '/tenant/notifications';
  }

  getRoleLabel(role?: string): string {
    if (!role) return '';
    if (role === 'Admin') return 'Super Admin';
    if (role === 'TenantAdmin') return 'Tenant Admin';
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
    if (role === 'Admin') {
      this.router.navigate(['/admin/settings']);
    } else {
      this.router.navigate(['/tenant/settings']);
    }
  }

  onLogout() {
    this.showProfile = false;
    this.router.navigate(['/logout']);
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
}
