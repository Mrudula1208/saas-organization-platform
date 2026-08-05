import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface PageNotification {
  id: string;
  message: string;
  date: string;
  status: 'Read' | 'Unread';
  project: string;
}

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications {
  activeTab: 'all' | 'unread' = 'all';
  selectedProject = 'All';

  notifications: PageNotification[] = [
    {
      id: 'notif-1',
      message: 'Task assigned for Website Redesign: "Task complex sinned tasks" has been assigned to Jann Sanner.',
      date: '2026-05-24 14:30',
      status: 'Unread',
      project: 'Website Redesign'
    },
    {
      id: 'notif-2',
      message: 'Milestone achieved for Database Migration: Database schema migration successfully verified.',
      date: '2026-05-23 11:15',
      status: 'Unread',
      project: 'Database Migration'
    },
    {
      id: 'notif-3',
      message: 'Task completed: Michael Brown finished "Automaticianane decision tasks" and updated progress.',
      date: '2026-05-22 09:00',
      status: 'Read',
      project: 'Database Migration'
    },
    {
      id: 'notif-4',
      message: 'Task Paused: Project manager put "Client Portal Integration" on hold due to requirement updates.',
      date: '2026-05-21 16:45',
      status: 'Read',
      project: 'Mobile App Development'
    },
    {
      id: 'notif-5',
      message: 'Task assigned for Mobile App: "Task name for itp created" has been assigned to Sarah Johnson.',
      date: '2026-05-20 10:30',
      status: 'Read',
      project: 'Mobile App Development'
    }
  ];

  // Unread count signal
  unreadCount = computed(() => {
    return this.notifications.filter(n => n.status === 'Unread').length;
  });

  // Filtered list based on active tab and project filter
  filteredNotifications = computed(() => {
    let result = [...this.notifications];
    
    if (this.activeTab === 'unread') {
      result = result.filter(n => n.status === 'Unread');
    }
    
    if (this.selectedProject !== 'All') {
      result = result.filter(n => n.project === this.selectedProject);
    }
    
    return result;
  });

  setTab(tab: 'all' | 'unread') {
    this.activeTab = tab;
  }

  filterByProject(event: Event) {
    const selectElement = event.target as HTMLSelectElement;
    this.selectedProject = selectElement.value;
  }

  hasUnread(): boolean {
    return this.unreadCount() > 0;
  }

  markRead(notif: PageNotification) {
    notif.status = 'Read';
  }

  deleteNotif(notif: PageNotification) {
    this.notifications = this.notifications.filter(n => n.id !== notif.id);
  }

  markAllRead() {
    this.notifications.forEach(n => n.status = 'Read');
  }

  clearAll() {
    this.notifications = [];
  }
}
