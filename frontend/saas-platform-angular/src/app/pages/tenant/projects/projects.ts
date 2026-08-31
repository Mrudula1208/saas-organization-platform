import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService, Project, ProjectMember } from '../../../core/services/project';
import { UserService, User } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class Projects implements OnInit {
  projects: Project[] = [];
  filteredProjects: Project[] = [];

  searchQuery = '';
  statusFilter = '';

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
    private auth: Auth
  ) {}

  ngOnInit() {
    this.loadProjects();
  }

  get canManageMembers(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin']);
  }

  loadProjects() {
    this.projectService.getProjects().subscribe({
      next: (data: Project[]) => {
        this.projects = data;
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    this.filteredProjects = this.projects.filter(p => {
      const matchesSearch = p.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            p.description.toLowerCase().includes(this.searchQuery.toLowerCase());
      
      const matchesStatus = this.statusFilter === '' || p.status === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
  }

  openCreateModal() {
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    this.newProject = { name: '', description: '', startDate: today, endDate: nextMonth, priority: 'Medium' };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  saveProject() {
    if (!this.newProject.name) return;

    this.projectService.createProject(this.newProject).subscribe({
      next: () => {
        this.loadProjects();
        this.closeCreateModal();
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
    this.userService.getUsers().subscribe({
      next: (data: User[]) => {
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