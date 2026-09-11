import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProjectService, Project, TaskItem } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';

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
  strokeDashoffset = 339; // SVG circle circum = 2 * PI * 54 = 339.29

  recentProjects: Project[] = [];
  recentActivities: Activity[] = [
    { id: '1', icon: 'person_add', color: 'var(--primary-color)', message: 'Sarah Johnson joined project Website Redesign', time: '10 minutes ago' },
    { id: '2', icon: 'task_alt', color: 'var(--success-color)', message: 'Emma Smith completed task "Automaticianane decision tasks"', time: '2 hours ago' },
    { id: '3', icon: 'folder_open', color: 'var(--accent-color)', message: 'Jann Sanner created project "Client Portal Integration"', time: 'Yesterday' }
  ];

  isLoading = true;

  constructor(
    private projectService: ProjectService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.loadTenantDashboard();
  }

  loadTenantDashboard() {
    this.isLoading = true;
    
    this.projectService.getProjects().subscribe({
      next: (projects: Project[]) => {
        this.totalProjects = projects.length;
        this.recentProjects = projects.slice(0, 3);

        this.projectService.getTasks().subscribe({
          next: (tasks: TaskItem[]) => {
            const tenantTasks = tasks.filter((t: TaskItem) => projects.some((p: Project) => p.id === t.projectId));
            this.activeTasksCount = tenantTasks.filter((t: TaskItem) => t.status !== 'Completed').length;
            this.completedTasksCount = tenantTasks.filter((t: TaskItem) => t.status === 'Completed').length;
            
            const totalTasks = tenantTasks.length;
            this.completionRate = totalTasks > 0 ? Math.round((this.completedTasksCount / totalTasks) * 100) : 0;
            
            // SVG dashoffset calculation: circum * (1 - completionRate / 100)
            this.strokeDashoffset = 339.29 - (339.29 * this.completionRate) / 100;

            this.userService.getUsers().subscribe({
              next: (users: any[]) => {
                this.totalUsers = users.length;
                this.isLoading = false;
              },
              error: () => {
                this.isLoading = false;
              }
            });
          },
          error: () => {
            this.isLoading = false;
          }
        });
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}

