import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Auth } from './auth';

export interface Project {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  priority: string;
  status: string;
  ownerId: string;
  ownerName: string;
  tenantId: string;
  progress: number;
}

export interface TaskItem {
  id: string;
  title: string;
  description?: string;
  projectName: string;
  projectId: string;
  assignedUserId?: string;
  assignedUserName?: string;
  assignedUserImage?: string;
  priority: string;
  dueDate: string;
  status: string; // 'To Do', 'In Progress', 'Completed'
}

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private readonly projectApiUrl = 'https://localhost:7134/api/Project';
  private readonly tasksApiUrl = 'https://localhost:7134/api/Tasks';

  // Local state cache for mock operations
  private mockProjects: Project[] = [
    {
      id: 'proj-1',
      name: 'Website Redesign',
      description: 'Revamping the core company website to match the new branding guidelines and improve conversion rates.',
      startDate: '2025-12-20T00:00:00Z',
      endDate: '2026-06-18T00:00:00Z',
      priority: 'High',
      status: 'In Progress',
      ownerId: 'user-1',
      ownerName: 'Jann Sanner',
      tenantId: '11112222-3333-4444-5555-666677778888',
      progress: 78
    },
    {
      id: 'proj-2',
      name: 'Mobile App Development',
      description: 'Building a cross-platform Flutter mobile application for client dashboard access and notification tracking.',
      startDate: '2026-01-10T00:00:00Z',
      endDate: '2026-08-30T00:00:00Z',
      priority: 'Medium',
      status: 'In Progress',
      ownerId: 'user-2',
      ownerName: 'Emma Smith',
      tenantId: '11112222-3333-4444-5555-666677778888',
      progress: 45
    },
    {
      id: 'proj-3',
      name: 'Database Migration',
      description: 'Migrating legacy on-prem databases to Microsoft Azure SQL Server instance for high availability.',
      startDate: '2026-03-01T00:00:00Z',
      endDate: '2026-04-15T00:00:00Z',
      priority: 'High',
      status: 'Completed',
      ownerId: 'user-3',
      ownerName: 'Michael Brown',
      tenantId: '11112222-3333-4444-5555-666677778888',
      progress: 100
    },
    {
      id: 'proj-4',
      name: 'Client Portal Integration',
      description: 'Setting up third-party SSO and client billing history details in the self-service web portal.',
      startDate: '2026-04-01T00:00:00Z',
      endDate: '2026-07-20T00:00:00Z',
      priority: 'Low',
      status: 'Pending',
      ownerId: 'user-1',
      ownerName: 'Jann Sanner',
      tenantId: '11112222-3333-4444-5555-666677778888',
      progress: 0
    }
  ];

  private mockTasks: TaskItem[] = [
    {
      id: 'task-1',
      title: 'Task complex sinned tasks',
      description: 'Fix the layout integration bugs across user settings and dashboard tabs.',
      projectName: 'Website Redesign',
      projectId: 'proj-1',
      assignedUserId: 'user-1',
      assignedUserName: 'Jann Sanner',
      assignedUserImage: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority', // matches PDF spelling screenshot
      dueDate: '2026-02-25',
      status: 'To Do'
    },
    {
      id: 'task-2',
      title: 'Recamme/rocicinkan design',
      description: 'Review the landing page designs and confirm the HSL gradients look correct.',
      projectName: 'Website Redesign',
      projectId: 'proj-1',
      assignedUserId: 'user-2',
      assignedUserName: 'Emma Smith',
      assignedUserImage: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-03-05',
      status: 'To Do'
    },
    {
      id: 'task-3',
      title: 'Develop owteiment enentitions',
      description: 'Implement the token parsing module within the core authentication guard.',
      projectName: 'Website Redesign',
      projectId: 'proj-1',
      assignedUserId: 'user-3',
      assignedUserName: 'Michael Brown',
      assignedUserImage: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-02-28',
      status: 'To Do'
    },
    {
      id: 'task-4',
      title: 'Task name for itp created',
      description: 'Wired HTTP routing client endpoints inside app config providers.',
      projectName: 'Mobile App Development',
      projectId: 'proj-2',
      assignedUserId: 'user-4',
      assignedUserName: 'Sarah Johnson',
      assignedUserImage: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-05-15',
      status: 'In Progress'
    },
    {
      id: 'task-5',
      title: 'Task paingeis tenant tasks',
      description: 'Create CRUD interfaces for organization plans and user tables.',
      projectName: 'Website Redesign',
      projectId: 'proj-1',
      assignedUserId: 'user-1',
      assignedUserName: 'Jann Sanner',
      assignedUserImage: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-03-20',
      status: 'In Progress'
    },
    {
      id: 'task-6',
      title: 'Finishho stop tenant tasks',
      description: 'Review EF core tracking bug details and documentation fixes.',
      projectName: 'Database Migration',
      projectId: 'proj-3',
      assignedUserId: 'user-2',
      assignedUserName: 'Emma Smith',
      assignedUserImage: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-04-10',
      status: 'Completed'
    },
    {
      id: 'task-7',
      title: 'Automaticianane decision tasks',
      description: 'Set up automated build pipelines for staging deployment.',
      projectName: 'Database Migration',
      projectId: 'proj-3',
      assignedUserId: 'user-3',
      assignedUserName: 'Michael Brown',
      assignedUserImage: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-04-12',
      status: 'Completed'
    },
    {
      id: 'task-8',
      title: 'Completed camp tenant tasks',
      description: 'Confirm localdb database schema matches migration script versions.',
      projectName: 'Database Migration',
      projectId: 'proj-3',
      assignedUserId: 'user-1',
      assignedUserName: 'Jann Sanner',
      assignedUserImage: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=100',
      priority: 'Priority',
      dueDate: '2026-04-14',
      status: 'Completed'
    }
  ];

  constructor(private http: HttpClient, private auth: Auth) {}

  private getHeaders(): HttpHeaders {
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        return new HttpHeaders().set('Authorization', `Bearer ${token}`);
      }
    }
    return new HttpHeaders();
  }

  /* Projects API wrappers */
  getProjects(): Observable<Project[]> {
    const tenantId = this.auth.getTenantId();
    return this.http.get<Project[]>(this.projectApiUrl, { headers: this.getHeaders() }).pipe(
      catchError(() => {
        console.warn('Project API offline. Loading mock projects.');
        if (!tenantId) return of([...this.mockProjects]);
        return of(this.mockProjects.filter(p => p.tenantId === tenantId));
      })
    );
  }

  createProject(project: any): Observable<Project> {
    const tenantId = this.auth.getTenantId() || '11112222-3333-4444-5555-666677778888';
    const newProject: Project = {
      id: crypto.randomUUID(),
      name: project.name,
      description: project.description || '',
      startDate: project.startDate || new Date().toISOString(),
      endDate: project.endDate || new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      priority: project.priority || 'Medium',
      status: 'Pending',
      ownerId: 'user-1',
      ownerName: 'Jann Sanner',
      tenantId: tenantId,
      progress: 0
    };

    return this.http.post<Project>(this.projectApiUrl, newProject, { headers: this.getHeaders() }).pipe(
      tap((res) => this.mockProjects.push(res)),
      catchError(() => {
        console.warn('Project API creation failed. Storing in local mock cache.');
        this.mockProjects.push(newProject);
        return of(newProject);
      })
    );
  }

  deleteProject(id: string): Observable<boolean> {
    return this.http.delete(`${this.projectApiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Project API deletion failed. Removing from mock cache: ${id}`);
        this.mockProjects = this.mockProjects.filter(p => p.id !== id);
        this.mockTasks = this.mockTasks.filter(t => t.projectId !== id);
        return of(true);
      })
    );
  }

  /* Tasks API wrappers */
  getTasks(projectId?: string): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(this.tasksApiUrl, { headers: this.getHeaders() }).pipe(
      catchError(() => {
        console.warn('Tasks API offline. Loading mock tasks.');
        let tasks = [...this.mockTasks];
        if (projectId) {
          tasks = tasks.filter(t => t.projectId === projectId);
        }
        return of(tasks);
      })
    );
  }

  createTask(task: any): Observable<TaskItem> {
    const project = this.mockProjects.find(p => p.id === task.projectId) || this.mockProjects[0];
    const newTask: TaskItem = {
      id: crypto.randomUUID(),
      title: task.title,
      description: task.description || '',
      projectName: project.name,
      projectId: task.projectId,
      assignedUserId: task.assignedUserId || 'user-1',
      assignedUserName: task.assignedUserName || 'Jann Sanner',
      assignedUserImage: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=100',
      priority: task.priority || 'Medium',
      dueDate: task.dueDate || new Date().toISOString().split('T')[0],
      status: 'To Do'
    };

    return this.http.post<TaskItem>(this.tasksApiUrl, newTask, { headers: this.getHeaders() }).pipe(
      tap((res) => this.mockTasks.push(res)),
      catchError(() => {
        console.warn('Tasks API creation failed. Storing in local mock cache.');
        this.mockTasks.push(newTask);
        return of(newTask);
      })
    );
  }

  updateTaskStatus(taskId: string, status: string): Observable<boolean> {
    // API put request to update task status
    return this.http.put(`${this.tasksApiUrl}/${taskId}/status`, { status }, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Tasks API status update failed. Updating mock task: ${taskId} to ${status}`);
        const idx = this.mockTasks.findIndex(t => t.id === taskId);
        if (idx !== -1) {
          this.mockTasks[idx].status = status;
          
          // Re-calculate project progress if updated in mock data
          const projectId = this.mockTasks[idx].projectId;
          const projTasks = this.mockTasks.filter(t => t.projectId === projectId);
          const completedCount = projTasks.filter(t => t.status === 'Completed').length;
          const projIdx = this.mockProjects.findIndex(p => p.id === projectId);
          if (projIdx !== -1 && projTasks.length > 0) {
            this.mockProjects[projIdx].progress = Math.round((completedCount / projTasks.length) * 100);
          }

          return of(true);
        }
        return of(false);
      })
    );
  }

  updateTask(taskId: string, task: any): Observable<boolean> {
    return this.http.put(`${this.tasksApiUrl}/${taskId}`, task, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Tasks API update failed. Updating mock task: ${taskId}`);
        const idx = this.mockTasks.findIndex(t => t.id === taskId);
        if (idx !== -1) {
          this.mockTasks[idx] = { ...this.mockTasks[idx], ...task };
          return of(true);
        }
        return of(false);
      })
    );
  }

  deleteTask(taskId: string): Observable<boolean> {
    return this.http.delete(`${this.tasksApiUrl}/${taskId}`, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Tasks API delete failed. Removing mock task: ${taskId}`);
        this.mockTasks = this.mockTasks.filter(t => t.id !== taskId);
        return of(true);
      })
    );
  }
}
