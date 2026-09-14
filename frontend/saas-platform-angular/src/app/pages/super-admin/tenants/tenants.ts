import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantService } from '../../../core/services/tenant';
import { Tenant } from '../../../models/tenant.model';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tenants.html',
  styleUrl: './tenants.css',
})
export class Tenants implements OnInit {
  filteredTenants: Tenant[] = [];
  
  // Search & Filter
  searchQuery = '';
  planFilter = '';
  errorMessage = '';

  // Server-side pagination: the API filters, sorts and counts in the database.
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Modals state
  isCreateModalOpen = false;
  isEditModalOpen = false;
  isViewModalOpen = false;

  // Form states
  newTenant = { name: '', plan: 'Basic', contactEmail: '', domain: '' };
  selectedTenant: Tenant | null = null;
  editTenantForm = { id: '', name: '', plan: '', contactEmail: '', domain: '', status: '' };

  constructor(private tenantService: TenantService) {}

  ngOnInit() {
    this.loadTenants();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  loadTenants() {
    this.loading = true;
    this.errorMessage = '';
    this.tenantService.getAll(this.page, this.pageSize, this.searchQuery, this.planFilter).subscribe({
      next: (res) => {
        // The current page disappeared (e.g. last row deleted): show the last page that still has rows.
        if (res.data.length === 0 && this.page > 1) {
          this.page = Math.max(1, Math.ceil(res.totalCount / this.pageSize));
          this.loadTenants();
          return;
        }
        this.filteredTenants = res.data;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getErrorMessage(err, 'Could not load tenants. Please try again later.');
      }
    });
  }

  onSearch() {
    // Ask the server only after the user stops typing.
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.loadTenants();
    }, 400);
  }

  onFilterChange() {
    this.page = 1;
    this.loadTenants();
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadTenants();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadTenants();
    }
  }

  // CREATE
  openCreateModal() {
    this.newTenant = { name: '', plan: 'Basic', contactEmail: '', domain: '' };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  onNewTenantNameChange() {
    if (this.newTenant.name) {
      this.newTenant.domain = `${this.newTenant.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`;
    } else {
      this.newTenant.domain = '';
    }
  }

  saveNewTenant() {
    if (!this.newTenant.name || !this.newTenant.contactEmail) return;

    this.tenantService.create(this.newTenant).subscribe({
      next: () => {
        this.loadTenants();
        this.closeCreateModal();
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not create the tenant.');
      }
    });
  }

  // VIEW DETAILS
  openViewModal(tenant: Tenant) {
    this.selectedTenant = tenant;
    this.isViewModalOpen = true;
  }

  closeViewModal() {
    this.selectedTenant = null;
    this.isViewModalOpen = false;
  }

  // EDIT
  openEditModal(tenant: Tenant) {
    this.editTenantForm = {
      id: tenant.id,
      name: tenant.name,
      plan: tenant.plan || 'Basic',
      contactEmail: tenant.contactEmail,
      domain: tenant.domain,
      status: tenant.status || 'Active'
    };
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
  }

  saveEditTenant() {
    if (!this.editTenantForm.name || !this.editTenantForm.id) return;

    this.tenantService.update(this.editTenantForm.id, this.editTenantForm).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadTenants();
          this.closeEditModal();
        }
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not update the tenant.');
      }
    });
  }

  // TOGGLE STATUS (Deactivate / Reactivate)
  toggleStatus(tenant: Tenant) {
    const updatedStatus = tenant.status === 'Suspended' || tenant.status?.includes('Deactiv') ? 'Active' : 'Suspended';
    const payload = { ...tenant, status: updatedStatus };
    
    this.tenantService.update(tenant.id, payload).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadTenants();
        }
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not change the tenant status.');
      }
    });
  }

  // DELETE
  deleteTenant(id: string) {
    if (confirm('Are you sure you want to delete this tenant organization? This action is permanent.')) {
      this.tenantService.delete(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadTenants();
          }
        },
        error: (err) => {
          this.errorMessage = getErrorMessage(err, 'Could not delete the tenant.');
        }
      });
    }
  }
}

