import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

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

export interface ProjectMember {
  id: string;
  projectId: string;
  projectName: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  userRole: string;
  userProfileImageUrl?: string;
}

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private readonly projectApiUrl = 'https://localhost:7134/api/Project';
  private readonly tasksApiUrl = 'https://localhost:7134/api/Tasks';
  private readonly projectMembersApiUrl = 'https://localhost:7134/api/ProjectMembers';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        return new HttpHeaders().set('Authorization', `Bearer ${token}`);
      }
    }
    return new HttpHeaders();
  }

  // Maps the API project shape into the shape the UI pages expect
  private mapProject(serverProject: any): Project {
    const tasks: any[] = serverProject.tasks || [];
    const completed = tasks.filter((t) => t.status === 'Completed' || t.isCompleted).length;

    return {
      id: serverProject.id,
      name: serverProject.name,
      description: serverProject.description || '',
      startDate: serverProject.startDate || '',
      endDate: serverProject.endDate || '',
      priority: serverProject.priority || 'Medium',
      status: serverProject.status || 'Pending',
      ownerId: serverProject.ownerId,
      ownerName: serverProject.owner?.fullName || '',
      tenantId: serverProject.tenantId,
      progress: tasks.length > 0 ? Math.round((completed / tasks.length) * 100) : 0
    };
  }

  // Maps the API task shape into the shape the UI pages expect
  private mapTask(serverTask: any): TaskItem {
    return {
      id: serverTask.id,
      title: serverTask.name || serverTask.title,
      description: serverTask.description || '',
      projectName: serverTask.project?.name || '',
      projectId: serverTask.projectId,
      assignedUserId: serverTask.assignedUserId,
      assignedUserName: serverTask.assignedUser?.fullName || '',
      priority: serverTask.priority || 'Medium',
      dueDate: serverTask.dueDate || '',
      status: serverTask.status
    };
  }

  /* Projects API wrappers */
  getProjects(): Observable<Project[]> {
    return this.http.get<any[]>(this.projectApiUrl, { headers: this.getHeaders() }).pipe(
      map((projects) => projects.map((project) => this.mapProject(project)))
    );
  }

  createProject(project: any): Observable<Project> {
    const payload = {
      name: project.name,
      description: project.description || '',
      startDate: project.startDate || new Date().toISOString(),
      endDate: project.endDate || new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      priority: project.priority || 'Medium',
      status: project.status || 'Pending'
    };

    return this.http.post<any>(this.projectApiUrl, payload, { headers: this.getHeaders() }).pipe(
      map((res) => this.mapProject(res))
    );
  }

  deleteProject(id: string): Observable<boolean> {
    return this.http.delete(`${this.projectApiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  /* Tasks API wrappers */
  getTasks(projectId?: string): Observable<TaskItem[]> {
    let url = this.tasksApiUrl;
    if (projectId) {
      url = `${this.tasksApiUrl}?projectId=${projectId}`;
    }
    return this.http.get<any[]>(url, { headers: this.getHeaders() }).pipe(
      map((tasks) => tasks.map((task) => this.mapTask(task)))
    );
  }

  createTask(task: any): Observable<TaskItem> {
    const payload = {
      name: task.title,
      description: task.description || '',
      projectId: task.projectId,
      assignedUserId: task.assignedUserId,
      status: task.status || 'To Do',
      priority: task.priority || 'Medium',
      dueDate: task.dueDate || new Date().toISOString().split('T')[0]
    };

    return this.http.post<any>(this.tasksApiUrl, payload, { headers: this.getHeaders() }).pipe(
      map((res) => this.mapTask(res))
    );
  }

  updateTaskStatus(taskId: string, status: string): Observable<boolean> {
    return this.http.patch(`${this.tasksApiUrl}/${taskId}/status`, { status }, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  updateTask(taskId: string, task: any): Observable<boolean> {
    const payload = {
      name: task.title || task.name,
      description: task.description || '',
      assignedUserId: task.assignedUserId,
      status: task.status,
      priority: task.priority,
      dueDate: task.dueDate,
      isCompleted: task.isCompleted
    };

    return this.http.put(`${this.tasksApiUrl}/${taskId}`, payload, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  deleteTask(taskId: string): Observable<boolean> {
    return this.http.delete(`${this.tasksApiUrl}/${taskId}`, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  /* Project Members API wrappers */
  getProjectMembers(projectId: string): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`${this.projectMembersApiUrl}/project/${projectId}`, {
      headers: this.getHeaders()
    });
  }

  addProjectMember(projectId: string, userId: string): Observable<ProjectMember> {
    const payload = { projectId, userId };
    return this.http.post<ProjectMember>(this.projectMembersApiUrl, payload, { headers: this.getHeaders() });
  }

  removeProjectMember(memberId: string): Observable<boolean> {
    return this.http.delete(`${this.projectMembersApiUrl}/${memberId}`, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }
}