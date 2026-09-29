import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../../../core/services/project';
import { UserService } from '../../../../core/services/user';
import { Project, ProjectMember, UpdateProjectPayload } from '../../../../models/project.model';
import { User } from '../../../../models/user.model';
import { Auth } from '../../../../core/services/auth';

@Component({
  selector: 'app-project-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './project-details.html',
  styleUrl: './project-details.css',
})
export class ProjectDetails implements OnInit {
  projectId = '';
  project: Project | null = null;

  loading = true;
  loadError = '';

  // Project Members
  members: ProjectMember[] = [];
  membersLoading = false;
  eligibleUsers: User[] = [];
  tenantUsers: User[] = [];
  selectedMemberUserId = '';
  addMemberLoading = false;
  memberError = '';

  // Edit modal state
  isEditModalOpen = false;
  saving = false;
  saveError = '';
  saveSuccess = false;
  fieldErrors: { [key: string]: string } = {};

  editForm: UpdateProjectPayload = {
    name: '',
    description: '',
    status: 'Pending',
    priority: 'Medium',
    startDate: '',
    endDate: '',
    isActive: true,
  };

  readonly statuses = [
    'Backlog',
    'Pending',
    'In Progress',
    'Active',
    'On Hold',
    'Completed',
    'Cancelled',
  ];
  readonly priorities = ['Low', 'Medium', 'High'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private projectService: ProjectService,
    private userService: UserService,
    private auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id') || '';

    if (!this.projectId) {
      this.loading = false;
      this.loadError = 'No project was specified.';
      return;
    }

    this.loadProject();
    this.loadMembers();
    this.loadTenantUsers();
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

  loadMembers(): void {
    if (!this.projectId) return;
    this.membersLoading = true;
    this.projectService.getProjectMembers(this.projectId).subscribe({
      next: (members) => {
        this.members = members;
        this.membersLoading = false;
        this.updateEligibleUsers();
        this.cdr.markForCheck();
      },
      error: () => {
        this.membersLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadTenantUsers(): void {
    if (!this.canManageMembers) return;
    this.userService.getUsers(1, 200).subscribe({
      next: (res) => {
        this.tenantUsers = res.data;
        this.updateEligibleUsers();
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  updateEligibleUsers(): void {
    const existingIds = new Set(this.members.map(m => m.userId));
    this.eligibleUsers = this.tenantUsers.filter(u => !existingIds.has(u.id));
  }

  addMember(): void {
    if (!this.selectedMemberUserId || !this.projectId) return;
    this.addMemberLoading = true;
    this.memberError = '';
    this.projectService.addProjectMember(this.projectId, this.selectedMemberUserId).subscribe({
      next: () => {
        this.addMemberLoading = false;
        this.selectedMemberUserId = '';
        this.loadMembers();
      },
      error: (err: any) => {
        this.addMemberLoading = false;
        this.memberError = this.extractErrorMessage(err, 'Failed to add project member.');
        this.cdr.markForCheck();
      }
    });
  }

  removeMember(memberId: string): void {
    if (!confirm('Are you sure you want to remove this member from the project?')) return;
    this.projectService.removeProjectMember(memberId).subscribe({
      next: () => {
        this.loadMembers();
      },
      error: (err: any) => {
        alert(this.extractErrorMessage(err, 'Failed to remove member.'));
      }
    });
  }

  loadProject(): void {
    this.loading = true;
    this.loadError = '';
    this.cdr.markForCheck();

    this.projectService.getProject(this.projectId).subscribe({
      next: (project: Project) => {
        this.project = project;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.project = null;
        this.loading = false;
        this.loadError = this.extractErrorMessage(
          err,
          'Unable to load this project. It may have been removed or you may not have access to it.'
        );
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/tenant/projects']);
  }

  openEditModal(): void {
    if (!this.project) return;

    this.editForm = {
      name: this.project.name,
      description: this.project.description || '',
      status: this.project.status || 'Pending',
      priority: this.project.priority || 'Medium',
      startDate: this.toDateInput(this.project.startDate),
      endDate: this.toDateInput(this.project.endDate),
      isActive: this.project.isActive,
    };

    this.fieldErrors = {};
    this.saveError = '';
    this.saveSuccess = false;
    this.isEditModalOpen = true;
  }

  closeEditModal(): void {
    if (this.saving) return;
    this.isEditModalOpen = false;
    this.saveError = '';
    this.fieldErrors = {};
  }

  validateEditForm(): boolean {
    const errors: { [key: string]: string } = {};

    if (!this.editForm.name || !this.editForm.name.trim()) {
      errors['name'] = 'Project name is required.';
    } else if (this.editForm.name.trim().length > 200) {
      errors['name'] = 'Project name must be 200 characters or fewer.';
    }

    if (
      this.editForm.startDate &&
      this.editForm.endDate &&
      this.editForm.endDate < this.editForm.startDate
    ) {
      errors['endDate'] = 'End date cannot be before the start date.';
    }

    if (!this.editForm.status) {
      errors['status'] = 'Status is required.';
    }

    this.fieldErrors = errors;
    return Object.keys(errors).length === 0;
  }

  saveProject(): void {
    if (!this.validateEditForm()) return;

    this.saving = true;
    this.saveError = '';

    const payload: UpdateProjectPayload = {
      name: this.editForm.name.trim(),
      description: this.editForm.description || '',
      status: this.editForm.status,
      priority: this.editForm.priority,
      startDate: this.editForm.startDate,
      endDate: this.editForm.endDate,
      isActive: this.editForm.isActive,
    };

    this.projectService.updateProject(this.projectId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.isEditModalOpen = false;
        this.saveSuccess = true;
        // Refresh the details from the API so the view reflects persisted data.
        this.loadProject();
      },
      error: (err: any) => {
        this.saving = false;
        this.saveError = this.extractErrorMessage(
          err,
          'Unable to save the project. Please review the form and try again.'
        );
      },
    });
  }

  deleteProject(): void {
    if (!this.project) return;
    if (
      !confirm(
        'Are you sure you want to delete this project? This will also remove all associated tasks.'
      )
    ) {
      return;
    }

    this.projectService.deleteProject(this.project.id).subscribe({
      next: () => this.router.navigate(['/tenant/projects']),
      error: (err: any) => {
        this.loadError = this.extractErrorMessage(err, 'Unable to delete this project.');
      },
    });
  }

  dismissSuccess(): void {
    this.saveSuccess = false;
  }

  private toDateInput(value: string): string {
    if (!value) return '';
    const date = new Date(value);
    if (isNaN(date.getTime())) return '';
    return date.toISOString().split('T')[0];
  }

  private extractErrorMessage(err: any, fallback: string): string {
    const body = err?.error;
    if (body && typeof body.message === 'string' && body.message) return body.message;
    if (body && typeof body === 'string') return body;

    if (err?.status === 404) return 'Project not found.';
    if (err?.status === 403) return 'You do not have access to this project.';

    return fallback;
  }
}
