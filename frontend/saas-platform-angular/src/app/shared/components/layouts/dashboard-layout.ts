import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../sidebar/sidebar';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, Sidebar, Navbar],
  template: `
    <div class="app-container">
      <app-sidebar></app-sidebar>
      <div class="sidebar-backdrop" (click)="closeSidebar()"></div>
      <div style="flex: 1; display: flex; flex-direction: column; min-width: 0;">
        <app-navbar></app-navbar>
        <main class="main-content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `
})
export class DashboardLayout {
  closeSidebar() {
    if (typeof window !== 'undefined') {
      document.body.classList.remove('sidebar-open');
    }
  }
}
