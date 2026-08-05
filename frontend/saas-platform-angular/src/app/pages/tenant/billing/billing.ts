import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Auth } from '../../../core/services/auth';
import { TenantService, Tenant } from '../../../core/services/tenant';

interface Invoice {
  id: string;
  date: string;
  amount: number;
  status: string;
}

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing.html',
  styleUrl: './billing.css',
})
export class Billing implements OnInit {
  currentTenant: Tenant | null = null;
  paymentMethod = 'Visa ending in 4242';
  nextBillingDate = '2026-06-25';
  
  invoices: Invoice[] = [
    { id: 'INV-2026-004', date: '2026-05-25', amount: 45, status: 'Paid' },
    { id: 'INV-2026-003', date: '2026-04-25', amount: 45, status: 'Paid' },
    { id: 'INV-2026-002', date: '2026-03-25', amount: 45, status: 'Paid' },
    { id: 'INV-2026-001', date: '2026-02-25', amount: 15, status: 'Paid' } // Basic initially
  ];

  isLoading = true;

  constructor(private auth: Auth, private tenantService: TenantService) {}

  ngOnInit() {
    this.loadBillingInfo();
  }

  loadBillingInfo() {
    this.isLoading = true;
    const tenantId = this.auth.getTenantId();
    if (!tenantId) {
      this.isLoading = false;
      return;
    }

    this.tenantService.getById(tenantId).subscribe({
      next: (tenant: Tenant | null) => {
        if (tenant) {
          this.currentTenant = tenant;
          
          // Adjust invoice list price to match the tenant's current active plan
          this.invoices.forEach((inv, index) => {
            if (index < 3) {
              inv.amount = tenant.monthlyRevenue || 45;
            }
          });
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  downloadInvoice(id: string) {
    alert(`Downloading Invoice ${id} as PDF...`);
  }
}

