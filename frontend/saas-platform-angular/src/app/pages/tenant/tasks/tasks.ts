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

  // Modals state
  isCreateModalOpen = false;
  newTask = { name: '', description: '', projectId: '', assignedUserId: '', priority: 'Medium', dueDate: '' };

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
    this.projectService.getProjects().subscribe({
      next: (projData: Project[]) => {
        this.projects = projData;
        if (this.projects.length > 0) {
          // Default to first project if available
          this.selectedProjectId = this.projects[0].id;
        }

        this.userService.getUsers().subscribe({
          next: (userData: User[]) => {
            this.users = userData;
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
    this.projectService.getTasks().subscribe({
      next: (taskData: TaskItem[]) => {
        this.allTasks = taskData;
        this.applyFilters();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not load tasks. Please try again later.');
      }
    });
  }

  applyFilters() {
    // Filter tasks by selected project and search query
    const filtered = this.allTasks.filter((t: TaskItem) => {
      const matchesProject = !this.selectedProjectId || t.projectId === this.selectedProjectId;
      const matchesSearch = !this.searchQuery || t.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            (t.description && t.description.toLowerCase().includes(this.searchQuery.toLowerCase()));
      return matchesProject && matchesSearch;
    });

    // Segment into Kanban columns
    this.todoTasks = filtered.filter((t: TaskItem) => t.status === 'To Do');
    this.inProgressTasks = filtered.filter((t: TaskItem) => t.status === 'In Progress');
    this.completedTasks = filtered.filter((t: TaskItem) => t.status === 'Completed');
  }

  onFilterChange() {
    this.applyFilters();
  }

  onSearch() {
    this.applyFilters();
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

