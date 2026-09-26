import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
import { Project, ProjectMember } from '../../../models/project.model';
import { User } from '../../../models/user.model';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class Projects implements OnInit {
  filteredProjects: Project[] = [];

  isLoading = false;
  loadError = '';
  createError = '';

  searchQuery = '';
  statusFilter = '';

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
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

  isCreateModalOpen = false;
  newProject = { name: '', description: '', startDate: '', endDate: '', priority: 'Medium' };

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
    private auth: Auth,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadProjects();
  }

  get canManageMembers(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin']);
  }

  get canCreateProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get canDeleteProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin']);
  }

  viewProject(id: string) {
    this.router.navigate(['/tenant/projects', id]);
  }

  loadProjects() {
    this.isLoading = true;
    this.loadError = '';

    this.projectService.getProjects(this.page, this.pageSize, this.searchQuery, this.statusFilter).subscribe({
      next: (res) => {
        // The current page disappeared (e.g. last row deleted): show the last page that still has rows.
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadProjects();
          return;
        }
        this.filteredProjects = res.data;
        this.totalCount = res.totalCount;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.isLoading = false;
        this.loadError = this.extractErrorMessage(err, 'Failed to load projects. Please try again.');
      }
    });
  }

  onSearch() {
    // Ask the server only after the user stops typing.
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadProjects();
    }, 400);
  }

  onFilterChange() {
    this.page = 1;
    this.loadProjects();
  }

  openCreateModal() {
    this.createError = '';
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    this.newProject = { name: '', description: '', startDate: today, endDate: nextMonth, priority: 'Medium' };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
    this.createError = '';
  }

  saveProject() {
    if (!this.newProject.name) return;

    this.createError = '';
    this.projectService.createProject(this.newProject).subscribe({
      next: () => {
        this.loadProjects();
        this.closeCreateModal();
      },
      error: (err) => {
        this.createError = this.extractErrorMessage(err, 'Failed to create project. Please verify organization subscription limits.');
      }
    });
  }

  deleteProject(id: string) {
    if (confirm('Are you sure you want to delete this project? This will also remove all associated tasks.')) {
      this.projectService.deleteProject(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadProjects();
          }
        }
      });
    }
  }

  // MEMBERS MANAGEMENT
  openMembersModal(project: Project) {
    this.membersProject = project;
    this.members = [];
    this.eligibleUsers = [];
    this.selectedMemberUserId = '';
    this.membersLoaded = false;
    this.membersError = '';
    this.loadTenantUsers();
    this.loadProjectMembers(project.id);
  }

  closeMembersModal() {
    this.membersProject = null;
  }

  loadTenantUsers() {
    // The members dropdown needs a wide list, so ask for one big page (the API caps page size).
    this.userService.getUsers(1, 200).subscribe({
      next: (res) => {
        const data = res.data;
        const tenantId = this.auth.getTenantId();
        this.tenantUsers = tenantId
          ? data.filter(u => u.tenantId === tenantId)
          : data;
        this.updateEligibleUsers();
      }
    });
  }

  loadProjectMembers(projectId: string) {
    this.membersLoading = true;
    this.membersLoaded = false;
    this.membersError = '';

    this.projectService.getProjectMembers(projectId).subscribe({
      next: (data: ProjectMember[]) => {
        this.members = data;
        this.membersLoading = false;
        this.membersLoaded = true;
        this.updateEligibleUsers();
      },
      error: (err: any) => {
        this.membersLoading = false;
        this.membersLoaded = true;
        this.membersError = this.extractErrorMessage(err, 'Failed to load project members.');
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
      }
    });
  }

  private extractErrorMessage(err: any, fallback: string): string {
    const body = err?.error;
    if (body && typeof body.message === 'string' && body.message) return body.message;
    if (body && typeof body === 'string') return body;
    return fallback;
  }
}