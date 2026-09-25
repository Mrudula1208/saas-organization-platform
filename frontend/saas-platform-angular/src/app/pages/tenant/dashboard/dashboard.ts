import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProjectService } from '../../../core/services/project';
import { Project } from '../../../models/project.model';
import { TaskItem } from '../../../models/task.model';
import { UserService } from '../../../core/services/user';
import { SystemLogService } from '../../../core/services/system-log';
import { SystemLog } from '../../../models/system-log.model';

interface Activity {
  id: string;
  icon: string;
  color: string;
  message: string;
  time: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  totalUsers = 0;
  totalProjects = 0;
  activeTasksCount = 0;
  completedTasksCount = 0;
  completionRate = 0;
  strokeDashoffset = 339; // SVG circle circumference: 2 * PI * 54 = 339.29

  recentProjects: Project[] = [];
  recentActivities: Activity[] = [];

  isLoading = true;

  constructor(
    private projectService: ProjectService,
    private userService: UserService,
    private systemLogService: SystemLogService
  ) {}

  ngOnInit() {
    this.loadTenantDashboard();
  }

  loadTenantDashboard() {
    this.isLoading = true;

    // Fetch projects for statistics and recent project boards
    this.projectService.getProjects(1, 200).subscribe({
      next: (res) => {
        const projects = res.data;
        this.totalProjects = res.totalCount;
        this.recentProjects = projects.slice(0, 3);

        // Fetch tasks to calculate completion progress
        this.projectService.getTasks(1, 200).subscribe({
          next: (taskRes) => {
            const tenantTasks = taskRes.data.filter((t: TaskItem) => projects.some((p: Project) => p.id === t.projectId));
            this.activeTasksCount = tenantTasks.filter((t: TaskItem) => t.status !== 'Completed').length;
            this.completedTasksCount = tenantTasks.filter((t: TaskItem) => t.status === 'Completed').length;

            const totalTasks = tenantTasks.length;
            this.completionRate = totalTasks > 0 ? Math.round((this.completedTasksCount / totalTasks) * 100) : 0;

            // SVG dashoffset calculation: circum * (1 - completionRate / 100)
            this.strokeDashoffset = 339.29 - (339.29 * this.completionRate) / 100;

            // Fetch team members count
            this.userService.getUsers(1, 200).subscribe({
              next: (userRes) => {
                this.totalUsers = userRes.totalCount;
                this.loadRecentActivities();
              },
              error: () => {
                this.loadRecentActivities();
              }
            });
          },
          error: () => {
            this.loadRecentActivities();
          }
        });
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private loadRecentActivities() {
    // Read the 5 most recent audit log actions for this workspace
    this.systemLogService.getLogs('', '', '', '', 1, 5).subscribe({
      next: (res) => {
        const logs = res.data || [];
        this.recentActivities = logs.map(log => this.mapLogToActivity(log));
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private mapLogToActivity(log: SystemLog): Activity {
    let icon = 'notifications';
    let color = 'var(--primary-color)';

    const action = log.action || '';
    if (action.startsWith('TASK')) {
      icon = 'task_alt';
      color = 'var(--success-color)';
    } else if (action.startsWith('PROJECT')) {
      icon = 'folder_open';
      color = 'var(--accent-color)';
    } else if (action.startsWith('USER') || action.startsWith('AUTH')) {
      icon = 'person';
      color = 'var(--primary-color)';
    } else if (action.startsWith('PAYMENT') || action.startsWith('BILLING')) {
      icon = 'payments';
      color = 'var(--warning-color)';
    }

    return {
      id: log.id,
      icon,
      color,
      message: log.description || log.action,
      time: this.formatRelativeTime(log.createdAt)
    };
  }

  private formatRelativeTime(dateStr: string): string {
    if (!dateStr) return 'Recently';

    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins === 1 ? '' : 's'} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;

    return date.toLocaleDateString();
  }
}
