import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification';
import { ProjectService } from '../../../core/services/project';
import { Auth } from '../../../core/services/auth';
import { Project } from '../../../models/project.model';
import { AppNotification } from '../../../models/notification.model';

export interface NotificationItem {
  id: string;
  message: string;
  date: string;
  status: 'Unread' | 'Read';
  projectId?: string;
  projectName?: string;
  selected?: boolean;
}

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications implements OnInit {
  // Filter states
  selectedProject = '';
  selectedStatus = 'All'; // 'All' | 'Unread' | 'Read'
  projectsList: Project[] = [];

  // Notifications master list
  allNotifications: NotificationItem[] = [];

  // Pagination state
  currentPage = 1;
  pageSize = 6;
  selectAll = false;

  successToast = '';

  constructor(
    public notifService: NotificationService,
    private projectService: ProjectService,
    private auth: Auth,
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
    this.loadProjects();
    this.loadBackendNotifications();
  }

  loadProjects() {
    this.projectService.getProjects(1, 50).subscribe({
      next: (res) => {
        this.projectsList = res.data || [];
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  loadBackendNotifications() {
    this.notifService.getNotifications().subscribe({
      next: (list) => {
        this.allNotifications = (list || []).map((n: AppNotification) => ({
          id: n.id,
          message: n.message,
          date: n.createdAt ? new Date(n.createdAt).toISOString().replace('T', ' ').substring(0, 16) : '',
          status: n.isRead ? 'Read' : 'Unread',
          projectName: this.findProjectName(n.message),
          selected: false
        }));
        const unread = this.allNotifications.filter(n => n.status === 'Unread').length;
        this.notifService.unreadCount.set(unread);
        this.cdr.markForCheck();
      },
      error: () => {
        this.allNotifications = [];
        this.cdr.markForCheck();
      }
    });
  }

  private findProjectName(message: string): string | undefined {
    if (!message || !this.projectsList) return undefined;
    const match = this.projectsList.find(p => p.name && message.toLowerCase().includes(p.name.toLowerCase()));
    return match ? match.name : undefined;
  }

  get filteredNotifications(): NotificationItem[] {
    let list = this.allNotifications;

    if (this.selectedProject) {
      list = list.filter(n => n.projectName === this.selectedProject || n.projectId === this.selectedProject);
    }

    if (this.selectedStatus === 'Unread') {
      list = list.filter(n => n.status === 'Unread');
    } else if (this.selectedStatus === 'Read') {
      list = list.filter(n => n.status === 'Read');
    }

    return list;
  }

  get paginatedNotifications(): NotificationItem[] {
    const list = this.filteredNotifications;
    const start = (this.currentPage - 1) * this.pageSize;
    return list.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredNotifications.length / this.pageSize));
  }

  get unreadTotal(): number {
    return this.allNotifications.filter(n => n.status === 'Unread').length;
  }

  toggleSelectAll() {
    this.selectAll = !this.selectAll;
    this.paginatedNotifications.forEach(n => n.selected = this.selectAll);
  }

  checkSingleSelect() {
    const pageItems = this.paginatedNotifications;
    this.selectAll = pageItems.length > 0 && pageItems.every(n => n.selected);
  }

  markAsRead(item: NotificationItem) {
    item.status = 'Read';
    this.notifService.markRead(item.id).subscribe({
      next: () => {
        const unread = this.allNotifications.filter(n => n.status === 'Unread').length;
        this.notifService.unreadCount.set(unread);
      }
    });
    this.showNotificationFeedback('Marked notification as read');
    this.cdr.markForCheck();
  }

  deleteNotification(item: NotificationItem) {
    this.allNotifications = this.allNotifications.filter(n => n.id !== item.id);
    this.notifService.deleteNotification(item.id).subscribe({
      next: () => {
        const unread = this.allNotifications.filter(n => n.status === 'Unread').length;
        this.notifService.unreadCount.set(unread);
      }
    });
    this.showNotificationFeedback('Notification removed');
    this.cdr.markForCheck();
  }

  markAllAsRead() {
    this.allNotifications.forEach(n => n.status = 'Read');
    this.notifService.markAllRead().subscribe({
      next: () => {
        this.notifService.unreadCount.set(0);
      }
    });
    this.showNotificationFeedback('All notifications marked as read');
    this.cdr.markForCheck();
  }

  deleteSelected() {
    const selected = this.allNotifications.filter(n => n.selected);
    if (selected.length === 0) return;

    selected.forEach(item => {
      this.notifService.deleteNotification(item.id).subscribe();
    });

    this.allNotifications = this.allNotifications.filter(n => !n.selected);
    const unread = this.allNotifications.filter(n => n.status === 'Unread').length;
    this.notifService.unreadCount.set(unread);
    this.selectAll = false;
    this.showNotificationFeedback(`Deleted ${selected.length} selected notifications`);
    this.cdr.markForCheck();
  }

  // Pagination navigation
  firstPage() {
    this.currentPage = 1;
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  lastPage() {
    this.currentPage = this.totalPages;
  }

  setPage(page: number) {
    this.currentPage = page;
  }

  private showNotificationFeedback(msg: string) {
    this.successToast = msg;
    setTimeout(() => {
      this.successToast = '';
      this.cdr.markForCheck();
    }, 3500);
  }
}
