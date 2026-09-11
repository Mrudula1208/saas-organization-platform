import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';

interface SidebarLink {
  path: string;
  icon: string;
  label: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  
  // Computes the navigation links depending on active user role
  navLinks = computed<SidebarLink[]>(() => {
    const user = this.auth.currentUser();
    if (!user) return [];

    if (user.role === 'Admin') {
      return [
        { path: '/admin/dashboard', icon: 'dashboard', label: 'Dashboard' },
        { path: '/admin/tenants', icon: 'corporate_fare', label: 'Tenants' },
        { path: '/admin/users', icon: 'group', label: 'All Users' },
        { path: '/admin/subscription-plans', icon: 'loyalty', label: 'Subscription Plans' },
        { path: '/admin/revenue', icon: 'analytics', label: 'Revenue Analytics' },
        { path: '/admin/system-logs', icon: 'terminal', label: 'System Logs' },
        { path: '/admin/reports', icon: 'monitoring', label: 'Reports' },
        { path: '/admin/settings', icon: 'settings', label: 'Settings' }
      ];
    } else {
      const links: SidebarLink[] = [
        { path: '/tenant/dashboard', icon: 'dashboard', label: 'Dashboard' },
        { path: '/tenant/users', icon: 'group', label: 'Users' },
        { path: '/tenant/projects', icon: 'folder_open', label: 'Projects' },
        { path: '/tenant/tasks', icon: 'assignment', label: 'Tasks' },
        { path: '/tenant/reports', icon: 'monitoring', label: 'Reports' },
        { path: '/tenant/settings', icon: 'settings', label: 'Settings' }
      ];

      // Add billing for TenantAdmin only
      if (user.role === 'TenantAdmin') {
        links.push({ path: '/tenant/billing', icon: 'receipt_long', label: 'Billing' });
      }
      
      links.push({ path: '/tenant/notifications', icon: 'notifications', label: 'Notifications' });

      return links;
    }
  });

  constructor(private auth: Auth, private router: Router) {}

  closeSidebarOnMobile() {
    if (typeof window !== 'undefined') {
      document.body.classList.remove('sidebar-open');
    }
  }

  onLogout() {
    this.closeSidebarOnMobile();
    this.router.navigate(['/logout']);
  }
}
