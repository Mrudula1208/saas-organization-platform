import { Component, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';

export interface DropdownNotification {
  id: string;
  message: string;
  date: string;
  status: 'Read' | 'Unread';
  type: 'info' | 'success' | 'warning' | 'danger';
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar {
  isDarkTheme = true;
  showNotifications = false;
  showProfile = false;

  // Unread count signal
  unreadCount = signal(3);

  // Quick lists of notifications for dropdown
  notifications: DropdownNotification[] = [
    {
      id: 'n-1',
      message: 'New Tenant "Globex Corp" created successfully.',
      date: '2 hours ago',
      status: 'Unread',
      type: 'success'
    },
    {
      id: 'n-2',
      message: 'System upgrade scheduled for next Sunday at 02:00 UTC.',
      date: '5 hours ago',
      status: 'Unread',
      type: 'warning'
    },
    {
      id: 'n-3',
      message: 'Failed login attempt detected from IP 192.168.1.144.',
      date: '1 day ago',
      status: 'Unread',
      type: 'danger'
    }
  ];

  constructor(private auth: Auth, private router: Router) {
    this.detectSystemTheme();
  }

  get user() {
    return this.auth.currentUser;
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
  }

  toggleProfile() {
    this.showProfile = !this.showProfile;
    this.showNotifications = false;
  }

  markAllAsRead() {
    this.notifications.forEach(n => n.status = 'Read');
    this.unreadCount.set(0);
  }

  readNotification(notif: DropdownNotification) {
    if (notif.status === 'Unread') {
      notif.status = 'Read';
      this.unreadCount.update(c => Math.max(0, c - 1));
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

  getNotifIcon(type: string): string {
    switch (type) {
      case 'success': return 'check_circle';
      case 'warning': return 'warning';
      case 'danger': return 'error';
      default: return 'info';
    }
  }

  getNotifBackground(type: string): string {
    switch (type) {
      case 'success': return 'rgba(16, 185, 129, 0.15)';
      case 'warning': return 'rgba(245, 158, 11, 0.15)';
      case 'danger': return 'rgba(239, 68, 68, 0.15)';
      default: return 'rgba(59, 130, 246, 0.15)';
    }
  }

  getNotifColor(type: string): string {
    switch (type) {
      case 'success': return '#34d399';
      case 'warning': return '#fbbf24';
      case 'danger': return '#f87171';
      default: return '#60a5fa';
    }
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

  // Click outside to close dropdowns
  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.profile-menu') && !target.closest('.nav-btn')) {
      this.showProfile = false;
      this.showNotifications = false;
    }
  }
}
