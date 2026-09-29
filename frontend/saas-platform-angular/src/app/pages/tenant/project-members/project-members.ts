import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { Auth } from '../../../core/services/auth';
import { Project, ProjectMember } from '../../../models/project.model';
import { User } from '../../../models/user.model';
import { getErrorMessage } from '../../../core/helpers';

export interface EnrichedProjectMember extends ProjectMember {
  userRole: string;
  joinedDate?: string;
}

@Component({
  selector: 'app-project-members',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './project-members.html',
  styleUrl: './project-members.css'
})
export class ProjectMembers implements OnInit {
  projects: Project[] = [];
  users: User[] = [];
  allEnrichedMembers: EnrichedProjectMember[] = [];
  filteredMembers: EnrichedProjectMember[] = [];

  // Filter criteria
  selectedProjectId = '';
  searchQuery = '';
  roleFilter = '';
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Add Member Modal State
  isAddModalOpen = false;
  selectedAddProjectId = '';
  selectedAddUserId = '';
  eligibleUsersForAdd: User[] = [];
  addMemberLoading = false;
  addMemberError = '';

  // Delete / Remove Modal State
  isRemoveModalOpen = false;
  removingMember: EnrichedProjectMember | null = null;
  removeLoading = false;

  constructor(
    private projectService: ProjectService,
    private userService: UserService,
    public auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Member';
  }

  get isTenantAdmin(): boolean {
    return this.auth.hasRole(['TenantAdmin', 'SuperAdmin']);
  }

  get isManager(): boolean {
    return this.auth.hasRole(['Manager']);
  }

  get isMember(): boolean {
    return this.auth.hasRole(['Member']);
  }

  get canManageMembers(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  // KPI Metrics
  get totalMembersCount(): number {
    return this.allEnrichedMembers.length;
  }

  get activeProjectsWithTeamCount(): number {
    const projectIds = new Set(this.allEnrichedMembers.map(m => m.projectId));
    return projectIds.size;
  }

  get managerCount(): number {
    return this.allEnrichedMembers.filter(m => m.userRole === 'Manager').length;
  }

  get contributorCount(): number {
    return this.allEnrichedMembers.filter(m => m.userRole === 'Member' || !m.userRole).length;
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.projectService.getProjects(1, 100).subscribe({
      next: (projRes) => {
        this.projects = projRes.data;
        this.cdr.markForCheck();

        this.userService.getUsers(1, 200).subscribe({
          next: (userRes) => {
            this.users = userRes.data;
            this.cdr.markForCheck();
            this.loadAllProjectMembers();
          },
          error: (err) => {
            this.errorMessage = getErrorMessage(err, 'Failed to load team directory.');
            this.loading = false;
            this.cdr.markForCheck();
          }
        });
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Failed to load projects.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadAllProjectMembers() {
    if (this.projects.length === 0) {
      this.allEnrichedMembers = [];
      this.applyFilters();
      this.loading = false;
      this.cdr.markForCheck();
      return;
    }

    const memberRequests = this.projects.map(p =>
      this.projectService.getProjectMembers(p.id)
    );

    let completedRequests = 0;
    const aggregated: EnrichedProjectMember[] = [];

    this.projects.forEach((proj) => {
      this.projectService.getProjectMembers(proj.id).subscribe({
        next: (members) => {
          members.forEach((m) => {
            const matchedUser = this.users.find(u => u.id === m.userId);
            aggregated.push({
              ...m,
              projectName: proj.name,
              userRole: matchedUser?.role || 'Member',
              joinedDate: matchedUser?.createdAt ? new Date(matchedUser.createdAt).toLocaleDateString() : 'Active Member'
            });
          });
          completedRequests++;
          if (completedRequests === this.projects.length) {
            this.allEnrichedMembers = aggregated;
            this.applyFilters();
            this.loading = false;
            this.cdr.markForCheck();
          }
        },
        error: () => {
          completedRequests++;
          if (completedRequests === this.projects.length) {
            this.allEnrichedMembers = aggregated;
            this.applyFilters();
            this.loading = false;
            this.cdr.markForCheck();
          }
        }
      });
    });
  }

  applyFilters() {
    let result = [...this.allEnrichedMembers];

    if (this.selectedProjectId) {
      result = result.filter(m => m.projectId === this.selectedProjectId);
    }

    if (this.roleFilter) {
      result = result.filter(m => (m.userRole || 'Member') === this.roleFilter);
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(m =>
        (m.userFullName && m.userFullName.toLowerCase().includes(q)) ||
        (m.userEmail && m.userEmail.toLowerCase().includes(q)) ||
        (m.projectName && m.projectName.toLowerCase().includes(q))
      );
    }

    this.filteredMembers = result;
    this.cdr.markForCheck();
  }

  onProjectFilterChange() {
    this.applyFilters();
  }

  onRoleFilterChange() {
    this.applyFilters();
  }

  onSearch() {
    this.applyFilters();
  }

  // Add Member Modal
  openAddModal(presetProjectId?: string) {
    this.selectedAddProjectId = presetProjectId || (this.selectedProjectId || (this.projects[0]?.id || ''));
    this.selectedAddUserId = '';
    this.addMemberError = '';
    this.updateEligibleUsersForAdd();
    this.isAddModalOpen = true;
    this.cdr.markForCheck();
  }

  closeAddModal() {
    this.isAddModalOpen = false;
    this.selectedAddUserId = '';
    this.addMemberError = '';
    this.cdr.markForCheck();
  }

  onAddProjectSelectChange() {
    this.selectedAddUserId = '';
    this.updateEligibleUsersForAdd();
  }

  updateEligibleUsersForAdd() {
    if (!this.selectedAddProjectId) {
      this.eligibleUsersForAdd = [...this.users];
      return;
    }
    const currentMemberUserIds = new Set(
      this.allEnrichedMembers
        .filter(m => m.projectId === this.selectedAddProjectId)
        .map(m => m.userId)
    );
    this.eligibleUsersForAdd = this.users.filter(u => !currentMemberUserIds.has(u.id));
    this.cdr.markForCheck();
  }

  confirmAddMember() {
    if (!this.selectedAddProjectId || !this.selectedAddUserId) {
      this.addMemberError = 'Please select both a project and a team member.';
      return;
    }

    this.addMemberLoading = true;
    this.addMemberError = '';
    this.cdr.markForCheck();

    this.projectService.addProjectMember(this.selectedAddProjectId, this.selectedAddUserId).subscribe({
      next: () => {
        this.addMemberLoading = false;
        this.closeAddModal();
        this.successMessage = 'Team member successfully assigned to the project.';
        setTimeout(() => { this.successMessage = ''; this.cdr.markForCheck(); }, 4000);
        this.loadData();
      },
      error: (err) => {
        this.addMemberLoading = false;
        this.addMemberError = getErrorMessage(err, 'Failed to assign member to project.');
        this.cdr.markForCheck();
      }
    });
  }

  // Remove Member Modal
  openRemoveModal(member: EnrichedProjectMember) {
    this.removingMember = member;
    this.isRemoveModalOpen = true;
    this.cdr.markForCheck();
  }

  closeRemoveModal() {
    this.isRemoveModalOpen = false;
    this.removingMember = null;
    this.removeLoading = false;
    this.cdr.markForCheck();
  }

  confirmRemoveMember() {
    if (!this.removingMember) return;

    this.removeLoading = true;
    this.cdr.markForCheck();

    this.projectService.removeProjectMember(this.removingMember.id).subscribe({
      next: () => {
        this.removeLoading = false;
        this.closeRemoveModal();
        this.successMessage = 'Member removed from project.';
        setTimeout(() => { this.successMessage = ''; this.cdr.markForCheck(); }, 4000);
        this.loadData();
      },
      error: (err) => {
        this.removeLoading = false;
        alert(getErrorMessage(err, 'Failed to remove member.'));
        this.closeRemoveModal();
      }
    });
  }

  // Helpers
  getUserInitials(name?: string): string {
    if (!name) return 'U';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  getAvatarColor(name?: string): string {
    const colors = ['#2563EB', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EC4899', '#6366F1'];
    if (!name) return colors[0];
    let hash = 0;
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return colors[Math.abs(hash) % colors.length];
  }

  getRoleBadgeClass(role?: string): string {
    switch (role?.toLowerCase()) {
      case 'tenantadmin':
      case 'admin':
        return 'badge-cyan';
      case 'manager':
        return 'badge-purple';
      case 'member':
        return 'badge-emerald';
      default:
        return 'badge-slate';
    }
  }
}
