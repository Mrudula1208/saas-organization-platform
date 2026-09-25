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
  searchQuery = '';
  errorMessage = '';

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
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

  // Drag & Drop State
  // Holds the reference to the task item that is currently being dragged by the user
  draggedTask: TaskItem | null = null;

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

  // The server already filtered by project and search text; this only splits the current page into Kanban columns.
  applyFilters() {
    this.todoTasks = this.allTasks.filter((t: TaskItem) => t.status === 'To Do');
    this.inProgressTasks = this.allTasks.filter((t: TaskItem) => t.status === 'In Progress');
    this.completedTasks = this.allTasks.filter((t: TaskItem) => t.status === 'Completed');
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

  // DRAG AND DROP HANDLERS

  // This method triggers when the user starts dragging a task card.
  // We store the task object reference in memory so we know which card is being moved.
  onDragStart(event: DragEvent, task: TaskItem) {
    this.draggedTask = task;
    // Set visual feedback (e.g., move effect)
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  // This method prevents default behavior when a card is dragged over a column.
  // By default, browsers do not allow drop events on general container elements.
  // Calling preventDefault() turns on the drop zone.
  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  // This method handles dropping the task card into a new status column.
  // We extract the stored task, verify it exists, and call the service to update status.
  onDrop(event: DragEvent, newStatus: string) {
    event.preventDefault();
    if (this.draggedTask && this.draggedTask.status !== newStatus) {
      this.moveTask(this.draggedTask, newStatus);
    }
    // Clear the reference once the operation is completed
    this.draggedTask = null;
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

