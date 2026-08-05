import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService, Project, TaskItem } from '../../../core/services/project';
import { UserService, User } from '../../../core/services/user';

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

  // Modals state
  isCreateModalOpen = false;
  newTask = { title: '', description: '', projectId: '', assignedUserId: '', priority: 'Medium', dueDate: '' };

  constructor(
    private projectService: ProjectService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
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
          }
        });
      }
    });
  }

  loadTasks() {
    this.projectService.getTasks().subscribe({
      next: (taskData: TaskItem[]) => {
        this.allTasks = taskData;
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    // Filter tasks by selected project and search query
    const filtered = this.allTasks.filter((t: TaskItem) => {
      const matchesProject = !this.selectedProjectId || t.projectId === this.selectedProjectId;
      const matchesSearch = !this.searchQuery || t.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
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
      title: '',
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
    if (!this.newTask.title || !this.newTask.projectId) return;

    const assigned = this.users.find(u => u.id === this.newTask.assignedUserId);
    const payload = {
      ...this.newTask,
      assignedUserName: assigned ? assigned.fullName : 'Jann Sanner'
    };

    this.projectService.createTask(payload).subscribe({
      next: () => {
        this.loadTasks();
        this.closeCreateModal();
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
      }
    });
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
        }
      });
    }
  }
}

