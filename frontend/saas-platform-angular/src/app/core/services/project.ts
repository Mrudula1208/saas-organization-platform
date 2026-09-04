import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Project, UpdateProjectPayload, ProjectMember } from '../../models/project.model';
import { TaskItem } from '../../models/task.model';

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private readonly projectApiUrl = 'http://localhost:5258/api/Project';
  private readonly tasksApiUrl = 'http://localhost:5258/api/Tasks';
  private readonly projectMembersApiUrl = 'http://localhost:5258/api/ProjectMembers';

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
    const taskCount = serverProject.taskCount ?? 0;
    const completedTaskCount = serverProject.completedTaskCount ?? 0;

    return {
      id: serverProject.id,
      name: serverProject.name,
      description: serverProject.description || '',
      startDate: serverProject.startDate || '',
      endDate: serverProject.endDate || '',
      priority: serverProject.priority || 'Medium',
      status: serverProject.status || 'Pending',
      ownerId: serverProject.ownerId,
      ownerName: serverProject.ownerName || serverProject.owner?.fullName || '',
      tenantId: serverProject.tenantId,
      progress: serverProject.progress ?? (taskCount > 0 ? Math.round((completedTaskCount / taskCount) * 100) : 0),
      isActive: serverProject.isActive !== undefined ? serverProject.isActive : true,
      createdAt: serverProject.createdAt || '',
      taskCount,
      completedTaskCount
    };
  }

  // Maps the API task shape into the shape the UI pages expect
  private mapTask(serverTask: any): TaskItem {
    return {
      id: serverTask.id,
      name: serverTask.name || serverTask.title,
      description: serverTask.description || '',
      status: serverTask.status,
      priority: serverTask.priority || 'Medium',
      dueDate: serverTask.dueDate || '',
      isCompleted: serverTask.isCompleted ?? false,
      completedAt: serverTask.completedAt || null,
      projectId: serverTask.projectId,
      assignedUserId: serverTask.assignedUserId,
      createdAt: serverTask.createdAt || '',
      projectName: serverTask.project?.name || '',
      assignedUserName: serverTask.assignedUser?.fullName || ''
    };
  }

  /* Projects API wrappers */
  getProjects(): Observable<Project[]> {
    return this.http.get<any[]>(this.projectApiUrl, { headers: this.getHeaders() }).pipe(
      map((projects) => projects.map((project) => this.mapProject(project)))
    );
  }

  getProject(id: string): Observable<Project> {
    return this.http.get<any>(`${this.projectApiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map((res) => this.mapProject(res))
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

  updateProject(id: string, project: UpdateProjectPayload): Observable<boolean> {
    const payload = {
      name: project.name,
      description: project.description || '',
      status: project.status,
      priority: project.priority,
      startDate: project.startDate,
      endDate: project.endDate,
      isActive: project.isActive
    };

    return this.http.put(`${this.projectApiUrl}/${id}`, payload, { headers: this.getHeaders() }).pipe(
      map(() => true)
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
      name: task.name || task.title,
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
      name: task.name || task.title,
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