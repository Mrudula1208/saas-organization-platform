import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
import { BillingService } from '../../../core/services/billing';
import { CurrentPlan } from '../../../models/payment.model';
import { Project, ProjectMember, UpdateProjectPayload } from '../../../models/project.model';
import { User } from '../../../models/user.model';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class Projects implements OnInit {
  filteredProjects: Project[] = [];

  isLoading = false;
  loadError = '';
  createError = '';
  editError = '';

  searchQuery = '';
  statusFilter = '';

  // Server-side pagination
  page = 1;
  pageSize = 7;
  totalCount = 0;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Visual analytics metrics
  overallProgress = 78;
  overallProgressDashOffset = 55.3;
  readonly circleCircumference = 2 * Math.PI * 34; // ~213.63

  // Selection
  selectAll = false;
  selectedProjectIds = new Set<string>();

  // Modals
  isCreateModalOpen = false;
  newProject = {
    name: '',
    description: '',
    startDate: '',
    endDate: '',
    priority: 'Medium',
    status: 'Active',
    ownerId: ''
  };

  isEditModalOpen = false;
  editProjectData = {
    id: '',
    name: '',
    description: '',
    startDate: '',
    endDate: '',
    priority: 'Medium',
    status: 'Active',
    isActive: true
  };
  editLoading = false;

  isDeleteModalOpen = false;
  deletingProject: Project | null = null;
  deleteLoading = false;

  // More options dropdown tracking
  activeMoreMenuId: string | null = null;

  // Subscription plan & quota guardrail state
  currentPlan: CurrentPlan | null = null;
  isUpgradeModalOpen = false;

  // Members modal state
  membersProject: Project | null = null;
  members: ProjectMember[] = [];
  tenantUsers: User[] = [];
  eligibleUsers: User[] = [];
  selectedMemberUserId = '';
  membersLoading = false;
  membersLoaded = false;
  membersError = '';
  addMemberLoading = false;

  constructor(
    private projectService: ProjectService,
    private userService: UserService,
    private billingService: BillingService,
    private auth: Auth,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadCurrentPlan();
    this.loadTenantUsers();
    this.loadProjects();
  }

  get canCreateProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get canEditProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get canDeleteProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin']);
  }

  get canManageMembers(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Member';
  }

  get isMember(): boolean {
    return this.auth.hasRole(['Member']);
  }

  get pageStartItem(): number {
    return this.totalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageEndItem(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.activeMoreMenuId = null;
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardShortcut(event: KeyboardEvent) {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      const searchInput = document.getElementById('project-search-input') as HTMLInputElement;
      if (searchInput) searchInput.focus();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
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

  get isAtProjectLimit(): boolean {
    if (!this.currentPlan || !this.currentPlan.maxProjects) return false;
    return this.totalCount >= this.currentPlan.maxProjects;
  }



  loadCurrentPlan() {
    if (!this.auth.hasRole(['SuperAdmin', 'TenantAdmin'])) return;
    this.billingService.getCurrentPlan().subscribe({
      next: (plan) => {
        this.currentPlan = plan;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  loadTenantUsers() {
    this.userService.getUsers(1, 200).subscribe({
      next: (res) => {
        const data = res.data;
        const tenantId = this.auth.getTenantId();
        this.tenantUsers = tenantId
          ? data.filter(u => u.tenantId === tenantId)
          : data;
        this.updateEligibleUsers();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  loadProjects() {
    this.isLoading = true;
    this.loadError = '';
    this.cdr.markForCheck();

    this.projectService.getProjects(this.page, this.pageSize, this.searchQuery, this.statusFilter).subscribe({
      next: (res) => {
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadProjects();
          return;
        }
        this.filteredProjects = res.data;
        this.totalCount = res.totalCount;
        this.updateOverallProgress();
        this.isLoading = false;
        this.selectAll = false;
        this.selectedProjectIds.clear();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.isLoading = false;
        this.loadError = this.extractErrorMessage(err, 'Failed to load projects. Please try again.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  updateOverallProgress() {
    if (this.filteredProjects.length > 0) {
      const sum = this.filteredProjects.reduce((acc, p) => acc + (p.progress || 0), 0);
      const avg = Math.round(sum / this.filteredProjects.length);
      this.overallProgress = avg > 0 ? avg : 78;
    } else {
      this.overallProgress = 78;
    }
    this.overallProgressDashOffset = this.circleCircumference * (1 - this.overallProgress / 100);
  }

  onSearch() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadProjects();
    }, 350);
  }

  onFilterChange() {
    this.page = 1;
    this.loadProjects();
  }

  setPage(p: number) {
    if (p >= 1 && p <= this.totalPages && p !== this.page) {
      this.page = p;
      this.loadProjects();
    }
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadProjects();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadProjects();
    }
  }

  // Row Selection
  toggleSelectAll() {
    this.selectAll = !this.selectAll;
    if (this.selectAll) {
      this.filteredProjects.forEach(p => this.selectedProjectIds.add(p.id));
    } else {
      this.selectedProjectIds.clear();
    }
  }

  toggleRowSelection(id: string, event: Event) {
    event.stopPropagation();
    if (this.selectedProjectIds.has(id)) {
      this.selectedProjectIds.delete(id);
      this.selectAll = false;
    } else {
      this.selectedProjectIds.add(id);
      if (this.selectedProjectIds.size === this.filteredProjects.length) {
        this.selectAll = true;
      }
    }
  }

  isRowSelected(id: string): boolean {
    return this.selectedProjectIds.has(id);
  }

  // View details
  viewProject(id: string) {
    this.router.navigate(['/tenant/projects', id]);
  }

  // Create Project
  openCreateModal() {
    if (this.isAtProjectLimit) {
      this.isUpgradeModalOpen = true;
      return;
    }
    this.createError = '';
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    this.newProject = {
      name: '',
      description: '',
      startDate: today,
      endDate: nextMonth,
      priority: 'Medium',
      status: 'Active',
      ownerId: this.tenantUsers.length > 0 ? this.tenantUsers[0].id : ''
    };
    this.isCreateModalOpen = true;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
    this.createError = '';
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveProject() {
    if (!this.newProject.name.trim()) {
      this.createError = 'Project name is required.';
      return;
    }

    this.createError = '';
    this.projectService.createProject(this.newProject).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadProjects();
      },
      error: (err) => {
        const errorMsg = this.extractErrorMessage(err, 'Failed to create project. Please verify organization limits.');
        if (errorMsg.toLowerCase().includes('reached the limit') || errorMsg.toLowerCase().includes('upgrade your subscription')) {
          this.closeCreateModal();
          this.isUpgradeModalOpen = true;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        } else {
          this.createError = errorMsg;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      }
    });
  }

  // Edit Project
  openEditModal(project: Project, event?: Event) {
    if (event) event.stopPropagation();
    this.editError = '';
    const start = project.startDate ? new Date(project.startDate).toISOString().split('T')[0] : '';
    const end = project.endDate ? new Date(project.endDate).toISOString().split('T')[0] : '';

    this.editProjectData = {
      id: project.id,
      name: project.name,
      description: project.description || '',
      startDate: start,
      endDate: end,
      priority: project.priority || 'Medium',
      status: project.status || 'Active',
      isActive: project.isActive ?? true
    };
    this.isEditModalOpen = true;
    this.activeMoreMenuId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.editError = '';
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  saveEditProject() {
    if (!this.editProjectData.name.trim()) {
      this.editError = 'Project name is required.';
      return;
    }

    this.editLoading = true;
    this.editError = '';

    const payload: UpdateProjectPayload = {
      name: this.editProjectData.name.trim(),
      description: this.editProjectData.description.trim(),
      status: this.editProjectData.status,
      priority: this.editProjectData.priority,
      startDate: this.editProjectData.startDate ? new Date(this.editProjectData.startDate).toISOString() : new Date().toISOString(),
      endDate: this.editProjectData.endDate ? new Date(this.editProjectData.endDate).toISOString() : new Date().toISOString(),
      isActive: this.editProjectData.isActive
    };

    this.projectService.updateProject(this.editProjectData.id, payload).subscribe({
      next: () => {
        this.editLoading = false;
        this.closeEditModal();
        this.loadProjects();
      },
      error: (err) => {
        this.editLoading = false;
        this.editError = this.extractErrorMessage(err, 'Failed to update project.');
        this.cdr.markForCheck();
      }
    });
  }

  // Delete Project
  openDeleteModal(project: Project, event?: Event) {
    if (event) event.stopPropagation();
    this.deletingProject = project;
    this.isDeleteModalOpen = true;
    this.activeMoreMenuId = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
    this.deletingProject = null;
    this.deleteLoading = false;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  confirmDeleteProject() {
    if (!this.deletingProject) return;

    this.deleteLoading = true;
    this.projectService.deleteProject(this.deletingProject.id).subscribe({
      next: (success) => {
        this.deleteLoading = false;
        if (success) {
          this.closeDeleteModal();
          this.loadProjects();
        }
      },
      error: (err) => {
        this.deleteLoading = false;
        alert(this.extractErrorMessage(err, 'Failed to delete project.'));
        this.closeDeleteModal();
      }
    });
  }

  // More menu
  toggleMoreMenu(projectId: string, event: Event) {
    event.stopPropagation();
    this.activeMoreMenuId = this.activeMoreMenuId === projectId ? null : projectId;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  quickUpdateStatus(project: Project, newStatus: string, event: Event) {
    event.stopPropagation();
    this.activeMoreMenuId = null;

    const payload: UpdateProjectPayload = {
      name: project.name,
      description: project.description || '',
      status: newStatus,
      priority: project.priority || 'Medium',
      startDate: project.startDate || new Date().toISOString(),
      endDate: project.endDate || new Date().toISOString(),
      isActive: project.isActive ?? true
    };

    this.projectService.updateProject(project.id, payload).subscribe({
      next: () => {
        project.status = newStatus;
        this.updateOverallProgress();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  // Members Management Modal
  openMembersModal(project: Project, event?: Event) {
    if (event) event.stopPropagation();
    this.membersProject = project;
    this.members = [];
    this.eligibleUsers = [];
    this.selectedMemberUserId = '';
    this.membersLoaded = false;
    this.membersError = '';
    this.activeMoreMenuId = null;
    this.loadTenantUsers();
    this.loadProjectMembers(project.id);
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  closeMembersModal() {
    this.membersProject = null;
    this.cdr.markForCheck();
    this.cdr.detectChanges();
  }

  loadProjectMembers(projectId: string) {
    this.membersLoading = true;
    this.membersLoaded = false;
    this.membersError = '';
    this.cdr.markForCheck();

    this.projectService.getProjectMembers(projectId).subscribe({
      next: (data: ProjectMember[]) => {
        this.members = data;
        this.membersLoading = false;
        this.membersLoaded = true;
        this.updateEligibleUsers();
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.membersLoading = false;
        this.membersLoaded = true;
        this.membersError = this.extractErrorMessage(err, 'Failed to load project members.');
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  updateEligibleUsers() {
    const memberUserIds = new Set(this.members.map(m => m.userId));
    this.eligibleUsers = this.tenantUsers.filter(u => !memberUserIds.has(u.id));
    if (!this.eligibleUsers.some(u => u.id === this.selectedMemberUserId)) {
      this.selectedMemberUserId = this.eligibleUsers.length > 0 ? this.eligibleUsers[0].id : '';
    }
  }

  addMember() {
    if (!this.membersProject || !this.selectedMemberUserId) return;

    this.addMemberLoading = true;
    this.membersError = '';

    this.projectService.addProjectMember(this.membersProject.id, this.selectedMemberUserId).subscribe({
      next: () => {
        this.addMemberLoading = false;
        this.loadProjectMembers(this.membersProject!.id);
      },
      error: (err: any) => {
        this.addMemberLoading = false;
        this.membersError = this.extractErrorMessage(err, 'Failed to add member.');
        this.cdr.markForCheck();
      }
    });
  }

  removeMember(memberId: string, memberName: string) {
    if (!confirm(`Remove ${memberName} from this project?`)) return;

    this.membersError = '';
    this.projectService.removeProjectMember(memberId).subscribe({
      next: () => {
        if (this.membersProject) {
          this.loadProjectMembers(this.membersProject.id);
        }
      },
      error: (err: any) => {
        this.membersError = this.extractErrorMessage(err, 'Failed to remove member.');
        this.cdr.markForCheck();
      }
    });
  }

  // Presentation helpers
  getOwnerInitials(name: string): string {
    if (!name) return 'PM';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  getOwnerColor(name: string): string {
    if (!name) return '#2563EB';
    const colors = ['#2563EB', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EC4899', '#6366F1'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }

  getStatusBadgeClass(status: string): string {
    switch ((status || '').toLowerCase()) {
      case 'completed':
        return 'badge-status-completed';
      case 'active':
        return 'badge-status-active';
      case 'pending':
      case 'in progress':
      case 'on hold':
        return 'badge-status-pending';
      default:
        return 'badge-status-neutral';
    }
  }

  private extractErrorMessage(err: any, fallback: string): string {
    const body = err?.error;
    if (body && typeof body.message === 'string' && body.message) return body.message;
    if (body && typeof body === 'string') return body;
    return fallback;
  }
}