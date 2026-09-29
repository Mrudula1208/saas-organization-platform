import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
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
  
  // Role scoping: 'all' vs 'my'
  filterScope: 'all' | 'my' = 'all';

  // Kanban columns
  todoTasks: TaskItem[] = [];
  inProgressTasks: TaskItem[] = [];
  completedTasks: TaskItem[] = [];
  filteredTasks: TaskItem[] = [];

  // Filter criteria matching Image 2
  selectedProjectId = '';
  selectedStatus = '';
  selectedPriority = 'All';
  selectedDueDate = '';
  searchQuery = '';
  errorMessage = '';

  // View state: 'both' (default matching Image 2) or 'table' or 'board'
  activeView: 'both' | 'table' | 'board' = 'both';

  // Selection
  selectAll = false;
  selectedTaskIds = new Set<string>();

  // Drag & Drop State Tracking
  draggedTask: TaskItem | null = null;
  draggedTaskId: string | null = null;
  dragOverColumn: string | null = null;

  // Server-side pagination
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Modals state
  isCreateModalOpen = false;
  newTask = {
    name: '',
    description: '',
    projectId: '',
    assignedUserId: '',
    priority: 'Medium',
    status: 'To Do',
    dueDate: ''
  };

  isEditModalOpen = false;
  editTask = {
    id: '',
    name: '',
    description: '',
    projectId: '',
    assignedUserId: '',
    priority: 'Medium',
    dueDate: '',
    status: 'To Do',
    isCompleted: false
  };

  isDeleteModalOpen = false;
  deletingTask: TaskItem | null = null;
  deleteLoading = false;

  activeMoreMenuTaskId: string | null = null;

  constructor(
    private projectService: ProjectService,
    private userService: UserService,
    public auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  get currentUserId(): string | null {
    return this.auth.currentUser()?.id || null;
  }

  get isMember(): boolean {
    return this.auth.hasRole(['Member']);
  }

  get isManager(): boolean {
    return this.auth.hasRole(['Manager']);
  }

  get isTenantAdmin(): boolean {
    return this.auth.hasRole(['TenantAdmin', 'SuperAdmin']);
  }

  get canCreateTask(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get canEditTask(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get canDeleteTask(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Member';
  }

  get displayTotalCount(): number {
    return this.filterScope === 'my' ? this.filteredTasks.length : this.totalCount;
  }

  get myTasksCount(): number {
    const uid = this.currentUserId;
    if (!uid) return 0;
    return this.allTasks.filter(t => t.assignedUserId === uid).length;
  }

  setFilterScope(scope: 'all' | 'my') {
    this.filterScope = scope;
    this.applyFilters();
  }

  ngOnInit() {
    if (this.isMember) {
      this.filterScope = 'my';
    }
    this.loadData();
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.activeMoreMenuTaskId = null;
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardShortcut(event: KeyboardEvent) {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      const input = document.getElementById('task-search-input') as HTMLInputElement;
      if (input) input.focus();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.displayTotalCount / this.pageSize));
  }

  get pageStartItem(): number {
    return this.displayTotalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageEndItem(): number {
    return Math.min(this.page * this.pageSize, this.displayTotalCount);
  }

  get pagesArray(): number[] {
    const total = this.totalPages;
    const current = this.page;
    const delta = 2;
    const range: number[] = [];

    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  }

  get completionPercentage(): number {
    const total = this.todoTasks.length + this.inProgressTasks.length + this.completedTasks.length;
    if (total === 0) return 0;
    return Math.round((this.completedTasks.length / total) * 100);
  }

  loadData() {
    this.errorMessage = '';
    this.projectService.getProjects(1, 200).subscribe({
      next: (res) => {
        this.projects = res.data;
        this.cdr.markForCheck();

        this.userService.getUsers(1, 200).subscribe({
          next: (userRes) => {
            this.users = userRes.data;
            this.cdr.markForCheck();
            this.loadTasks();
          },
          error: (err) => {
            this.errorMessage = getErrorMessage(err, 'Could not load team members.');
            this.cdr.markForCheck();
            this.loadTasks();
          }
        });
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not load projects. Please try again later.');
        this.cdr.markForCheck();
      }
    });
  }

  loadTasks() {
    this.loading = true;
    this.cdr.markForCheck();

    this.projectService.getTasks(this.page, this.pageSize, this.selectedProjectId, this.searchQuery).subscribe({
      next: (res) => {
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadTasks();
          return;
        }
        this.allTasks = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.selectAll = false;
        this.selectedTaskIds.clear();
        this.applyFilters();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load tasks. Please try again later.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // Filter tasks based on status, priority, due date, etc.
  applyFilters() {
    let filtered = this.allTasks;

    if (this.filterScope === 'my') {
      const uid = this.currentUserId;
      if (uid) {
        filtered = filtered.filter(t => t.assignedUserId === uid);
      }
    }

    if (this.selectedPriority && this.selectedPriority !== 'All') {
      filtered = filtered.filter(t => (t.priority || '').toLowerCase() === this.selectedPriority.toLowerCase());
    }

    if (this.selectedStatus && this.selectedStatus !== '') {
      filtered = filtered.filter(t => {
        const s = (t.status || '').toLowerCase();
        const sel = this.selectedStatus.toLowerCase();
        if (sel === 'to do' || sel === 'awaited') {
          return s === 'to do' || s === 'awaited' || s === 'pending';
        }
        return s === sel;
      });
    }

    if (this.selectedDueDate) {
      filtered = filtered.filter(t => {
        if (!t.dueDate) return false;
        return t.dueDate.startsWith(this.selectedDueDate);
      });
    }

    this.filteredTasks = filtered;
    this.todoTasks = filtered.filter((t: TaskItem) => t.status === 'To Do' || t.status === 'Awaited' || t.status === 'Pending');
    this.inProgressTasks = filtered.filter((t: TaskItem) => t.status === 'In Progress');
    this.completedTasks = filtered.filter((t: TaskItem) => t.status === 'Completed');

    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  onFilterChange() {
    this.page = 1;
    this.loadTasks();
  }

  onSearch() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadTasks();
    }, 350);
  }

  switchView(view: 'both' | 'table' | 'board') {
    this.activeView = view;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  // Checkbox selection
  toggleSelectAll() {
    this.selectAll = !this.selectAll;
    if (this.selectAll) {
      this.allTasks.forEach(t => this.selectedTaskIds.add(t.id));
    } else {
      this.selectedTaskIds.clear();
    }
  }

  toggleRowSelection(id: string, event: Event) {
    event.stopPropagation();
    if (this.selectedTaskIds.has(id)) {
      this.selectedTaskIds.delete(id);
      this.selectAll = false;
    } else {
      this.selectedTaskIds.add(id);
      if (this.selectedTaskIds.size === this.allTasks.length) {
        this.selectAll = true;
      }
    }
  }

  isRowSelected(id: string): boolean {
    return this.selectedTaskIds.has(id);
  }

  // Pagination
  setPage(p: number) {
    if (p >= 1 && p <= this.totalPages && p !== this.page) {
      this.page = p;
      this.loadTasks();
    }
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

  // CREATE TASK
  openCreateModal(columnStatus: string = 'To Do') {
    const today = new Date().toISOString().split('T')[0];
    this.newTask = {
      name: '',
      description: '',
      projectId: this.selectedProjectId || (this.projects.length > 0 ? this.projects[0].id : ''),
      assignedUserId: this.users.length > 0 ? this.users[0].id : '',
      priority: 'Medium',
      status: columnStatus,
      dueDate: today
    };
    this.isCreateModalOpen = true;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveTask() {
    if (!this.newTask.name.trim() || !this.newTask.projectId) return;

    this.projectService.createTask(this.newTask).subscribe({
      next: () => {
        this.loadTasks();
        this.closeCreateModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not create the task.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // EDIT TASK
  openEditModal(task: TaskItem, event?: Event) {
    if (!this.canEditTask) return;
    if (event) event.stopPropagation();
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
    this.activeMoreMenuTaskId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveEditTask() {
    if (!this.editTask.name.trim() || !this.editTask.id) return;

    this.editTask.isCompleted = this.editTask.status === 'Completed';

    this.projectService.updateTask(this.editTask.id, this.editTask).subscribe({
      next: () => {
        this.loadTasks();
        this.closeEditModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the task.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // MOVE STATUS
  moveTask(task: TaskItem, newStatus: string, event?: Event) {
    if (event) event.stopPropagation();
    this.projectService.updateTaskStatus(task.id, newStatus).subscribe({
      next: (success: boolean) => {
        if (success) {
          task.status = newStatus;
          task.isCompleted = newStatus === 'Completed';
          this.applyFilters();
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not change the task status.');
        this.cdr.markForCheck();
      }
    });
  }

  markComplete(task: TaskItem, event?: Event) {
    if (event) event.stopPropagation();
    this.moveTask(task, 'Completed');
  }

  // DELETE TASK
  openDeleteModal(task: TaskItem, event?: Event) {
    if (event) event.stopPropagation();
    this.deletingTask = task;
    this.isDeleteModalOpen = true;
    this.activeMoreMenuTaskId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
    this.deletingTask = null;
    this.deleteLoading = false;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  confirmDeleteTask() {
    if (!this.deletingTask) return;

    this.deleteLoading = true;
    this.projectService.deleteTask(this.deletingTask.id).subscribe({
      next: (success: boolean) => {
        this.deleteLoading = false;
        if (success) {
          this.closeDeleteModal();
          this.loadTasks();
        }
      },
      error: (err) => {
        this.deleteLoading = false;
        alert(getErrorMessage(err, 'Could not delete the task.'));
        this.closeDeleteModal();
      }
    });
  }

  // DRAG AND DROP
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
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  onDragOver(event: DragEvent, columnStatus: string) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    if (this.dragOverColumn !== columnStatus) {
      this.dragOverColumn = columnStatus;
      this.cdr.markForCheck();
    }
  }

  onDragLeave(event: DragEvent, columnStatus: string) {
    const related = event.relatedTarget as HTMLElement;
    const current = event.currentTarget as HTMLElement;
    if (!current || !current.contains(related)) {
      if (this.dragOverColumn === columnStatus) {
        this.dragOverColumn = null;
        this.cdr.markForCheck();
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
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  // More menu
  toggleMoreMenu(taskId: string, event: Event) {
    event.stopPropagation();
    this.activeMoreMenuTaskId = this.activeMoreMenuTaskId === taskId ? null : taskId;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  // Helpers
  getUserInitials(name?: string): string {
    if (!name || !name.trim()) return 'UN';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  getUserColor(name?: string): string {
    if (!name) return '#2563EB';
    const colors = ['#2563EB', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EC4899', '#6366F1'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }

  getStatusBadgeClass(status?: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'completed') return 'status-completed';
    if (s === 'in progress') return 'status-in-progress';
    return 'status-awaited';
  }

  getPriorityBadgeClass(priority?: string): string {
    const p = (priority || '').toLowerCase();
    if (p === 'high') return 'priority-high';
    if (p === 'medium') return 'priority-medium';
    return 'priority-low';
  }

  getTaskProgressPercent(status?: string): number {
    const s = (status || '').toLowerCase();
    if (s === 'completed') return 100;
    if (s === 'in progress') return 55;
    return 15;
  }
}
