import { Component, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, AppNotification } from '../../../core/services/notification';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications implements OnInit {
  activeTab: 'all' | 'unread' = 'all';

  constructor(public notifService: NotificationService) {}

  ngOnInit() {
    this.notifService.loadNotifications();
  }

  get notifications() {
    return this.notifService.notifications;
  }

  get unreadCount() {
    return this.notifService.unreadCount;
  }

  get filteredNotifications() {
    const list = this.notifications();
    if (this.activeTab === 'unread') {
      return list.filter(n => !n.isRead);
    }
    return list;
  }

  setTab(tab: 'all' | 'unread') {
    this.activeTab = tab;
  }

  hasUnread(): boolean {
    return this.unreadCount() > 0;
  }

  markRead(notif: AppNotification) {
    this.notifService.markRead(notif.id).subscribe();
  }

  deleteNotif(notif: AppNotification) {
    this.notifService.deleteNotification(notif.id).subscribe();
  }

  markAllRead() {
    this.notifService.markAllRead().subscribe();
  }

  clearAll() {
    const all = this.notifications();
    for (const notif of all) {
      this.notifService.deleteNotification(notif.id).subscribe();
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
      + ' ' + d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  }
}
