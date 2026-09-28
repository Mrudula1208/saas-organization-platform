import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { Project } from '../../../models/project.model';
import { TaskItem } from '../../../models/task.model';
import { User } from '../../../models/user.model';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tasks.html',
  styleUrl: './tasks.css',
})
export class Tasks implements OnInit {
  projects: Project[] = [];
  users: User[] = [];
  allTasks: TaskItem[] = [];
  
  // Columns for Kanban
  todoTasks: TaskItem[] = [];
  inProgressTasks: TaskItem[] = [];
  completedTasks: TaskItem[] = [];

  // Toggle & Filters
  activeView: 'board' | 'table' = 'board';
  selectedProjectId = '';
  selectedPriority = 'All';
  searchQuery = '';
  errorMessage = '';

  // Drag & Drop State Tracking
  draggedTask: TaskItem | null = null;
  draggedTaskId: string | null = null;
  dragOverColumn: string | null = null;

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get completionPercentage(): number {
    const total = this.todoTasks.length + this.inProgressTasks.length + this.completedTasks.length;
    if (total === 0) return 0;
    return Math.round((this.completedTasks.length / total) * 100);
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadTasks();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadTasks();
    }
  }

  // Modals state
  isCreateModalOpen = false;
  newTask = { name: '', description: '', projectId: '', assignedUserId: '', priority: 'Medium', dueDate: '' };

  isEditModalOpen = false;
  editTask = { id: '', name: '', description: '', projectId: '', assignedUserId: '', priority: 'Medium', dueDate: '', status: 'To Do', isCompleted: false };

  constructor(
    private projectService: ProjectService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.errorMessage = '';
    // Dropdowns need a wide list, so ask for one big page (the API caps page size).
    this.projectService.getProjects(1, 200).subscribe({
      next: (res) => {
        this.projects = res.data;
        if (this.projects.length > 0) {
          // Default to first project if available
          this.selectedProjectId = this.projects[0].id;
        }

        this.userService.getUsers(1, 200).subscribe({
          next: (userRes) => {
            this.users = userRes.data;
            this.loadTasks();
          },
          error: (err) => {
            this.errorMessage = getErrorMessage(err, 'Could not load team members.');
            this.loadTasks();
          }
        });
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not load projects. Please try again later.');
      }
    });
  }

  loadTasks() {
    this.loading = true;
    this.projectService.getTasks(this.page, this.pageSize, this.selectedProjectId, this.searchQuery).subscribe({
      next: (res) => {
        // The current page disappeared: show the last page that still has rows.
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadTasks();
          return;
        }
        this.allTasks = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.applyFilters();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load tasks. Please try again later.');
      }
    });
  }

  // Filter tasks based on selected priority and split into Kanban columns
  applyFilters() {
    let filtered = this.allTasks;
    if (this.selectedPriority && this.selectedPriority !== 'All') {
      filtered = filtered.filter(t => (t.priority || '').toLowerCase() === this.selectedPriority.toLowerCase());
    }
    this.todoTasks = filtered.filter((t: TaskItem) => t.status === 'To Do');
    this.inProgressTasks = filtered.filter((t: TaskItem) => t.status === 'In Progress');
    this.completedTasks = filtered.filter((t: TaskItem) => t.status === 'Completed');
  }

  setPriorityFilter(priority: string) {
    this.selectedPriority = priority;
    this.applyFilters();
  }

  getUserInitials(name?: string): string {
    if (!name || !name.trim()) return 'UN';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  getUserColor(name?: string): string {
    if (!name) return '#6366F1';
    const colors = ['#6366F1', '#3B82F6', '#10B981', '#F59E0B', '#EC4899', '#8B5CF6', '#0EA5E9'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }

  isOverdue(dueDate?: string, status?: string): boolean {
    if (!dueDate || status === 'Completed') return false;
    const due = new Date(dueDate);
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    return due < now;
  }

  onFilterChange() {
    this.page = 1;
    this.loadTasks();
  }

  onSearch() {
    // Ask the server only after the user stops typing.
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadTasks();
    }, 400);
  }

  switchView(view: 'board' | 'table') {
    this.activeView = view;
  }

  // CREATE TASK
  openCreateModal() {
    const today = new Date().toISOString().split('T')[0];
    this.newTask = {
      name: '',
      description: '',
      projectId: this.selectedProjectId || (this.projects.length > 0 ? this.projects[0].id : ''),
      assignedUserId: this.users.length > 0 ? this.users[0].id : '',
      priority: 'Medium',
      dueDate: today
    };
    this.isCreateModalOpen = true;
  }

  openCreateModalForColumn(columnStatus: string) {
    this.openCreateModal();
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  saveTask() {
    if (!this.newTask.name || !this.newTask.projectId) return;

    // The backend resolves the assignee from assignedUserId; no fake fallback name is sent.
    this.projectService.createTask(this.newTask).subscribe({
      next: () => {
        this.loadTasks();
        this.closeCreateModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not create the task.');
      }
    });
  }

  // EDIT TASK
  openEditModal(task: TaskItem) {
    let formattedDue = '';
    if (task.dueDate) {
      const d = new Date(task.dueDate);
      formattedDue = !isNaN(d.getTime()) ? d.toISOString().split('T')[0] : '';
    }
    this.editTask = {
      id: task.id,
      name: task.name,
      description: task.description || '',
      projectId: task.projectId,
      assignedUserId: task.assignedUserId || (this.users.length > 0 ? this.users[0].id : ''),
      priority: task.priority || 'Medium',
      dueDate: formattedDue,
      status: task.status || 'To Do',
      isCompleted: task.isCompleted || task.status === 'Completed'
    };
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
  }

  saveEditTask() {
    if (!this.editTask.name || !this.editTask.id) return;

    this.editTask.isCompleted = this.editTask.status === 'Completed';

    this.projectService.updateTask(this.editTask.id, this.editTask).subscribe({
      next: () => {
        this.loadTasks();
        this.closeEditModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the task.');
      }
    });
  }

  // MOVE STATE
  moveTask(task: TaskItem, newStatus: string) {
    this.projectService.updateTaskStatus(task.id, newStatus).subscribe({
      next: (success: boolean) => {
        if (success) {
          task.status = newStatus;
          this.applyFilters();
        }
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not change the task status.');
      }
    });
  }

  // DRAG AND DROP HANDLERS WITH GLOW FEEDBACK
  onDragStart(event: DragEvent, task: TaskItem) {
    this.draggedTask = task;
    this.draggedTaskId = task.id;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', task.id);
    }
  }

  onDragEnd() {
    this.draggedTask = null;
    this.draggedTaskId = null;
    this.dragOverColumn = null;
  }

  onDragOver(event: DragEvent, columnStatus: string) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    if (this.dragOverColumn !== columnStatus) {
      this.dragOverColumn = columnStatus;
    }
  }

  onDragLeave(event: DragEvent, columnStatus: string) {
    const related = event.relatedTarget as HTMLElement;
    const current = event.currentTarget as HTMLElement;
    if (!current || !current.contains(related)) {
      if (this.dragOverColumn === columnStatus) {
        this.dragOverColumn = null;
      }
    }
  }

  onDrop(event: DragEvent, newStatus: string) {
    event.preventDefault();
    this.dragOverColumn = null;
    if (this.draggedTask && this.draggedTask.status !== newStatus) {
      this.moveTask(this.draggedTask, newStatus);
    }
    this.draggedTask = null;
    this.draggedTaskId = null;
  }

  // ACTIONS
  markComplete(task: TaskItem) {
    this.moveTask(task, 'Completed');
  }

  deleteTask(id: string) {
    if (confirm('Are you sure you want to delete this task?')) {
      this.projectService.deleteTask(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadTasks();
          }
        },
        error: (err) => {
          this.errorMessage = getErrorMessage(err, 'Could not delete the task.');
        }
      });
    }
  }
}

