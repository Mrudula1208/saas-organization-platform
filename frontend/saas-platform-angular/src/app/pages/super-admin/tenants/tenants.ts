import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantService, Tenant } from '../../../core/services/tenant';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tenants.html',
  styleUrl: './tenants.css',
})
export class Tenants implements OnInit {
  tenants: Tenant[] = [];
  filteredTenants: Tenant[] = [];
  
  // Search & Filter
  searchQuery = '';
  planFilter = '';

  // Modals state
  isCreateModalOpen = false;
  isEditModalOpen = false;
  isViewModalOpen = false;

  // Form states
  newTenant = { name: '', plan: 'Basic', emailAddress: '', domain: '' };
  selectedTenant: Tenant | null = null;
  editTenantForm = { id: '', name: '', plan: '', emailAddress: '', domain: '', status: '' };

  constructor(private tenantService: TenantService) {}

  ngOnInit() {
    this.loadTenants();
  }

  loadTenants() {
    this.tenantService.getAll().subscribe({
      next: (data: Tenant[]) => {
        this.tenants = data;
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    this.filteredTenants = this.tenants.filter((t: Tenant) => {
      const matchesSearch = t.name.toLowerCase().includes(this.searchQuery.toLowerCase()) || 
                            t.domain.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            t.emailAddress.toLowerCase().includes(this.searchQuery.toLowerCase());
      
      const matchesPlan = this.planFilter === '' || t.plan === this.planFilter;

      return matchesSearch && matchesPlan;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
  }

  // CREATE
  openCreateModal() {
    this.newTenant = { name: '', plan: 'Basic', emailAddress: '', domain: '' };
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
    if (!this.newTenant.name || !this.newTenant.emailAddress) return;

    this.tenantService.create(this.newTenant).subscribe({
      next: () => {
        this.loadTenants();
        this.closeCreateModal();
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
      plan: tenant.plan,
      emailAddress: tenant.emailAddress,
      domain: tenant.domain,
      status: tenant.status
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
      }
    });
  }

  // TOGGLE STATUS (Deactivate / Reactivate)
  toggleStatus(tenant: Tenant) {
    const updatedStatus = tenant.status === 'Suspended' || tenant.status.includes('Deactiv') ? 'Active' : 'Suspended';
    const payload = { ...tenant, status: updatedStatus };
    
    this.tenantService.update(tenant.id, payload).subscribe({
      next: (success: boolean) => {
        if (success) {
          this.loadTenants();
        }
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
        }
      });
    }
  }
}

