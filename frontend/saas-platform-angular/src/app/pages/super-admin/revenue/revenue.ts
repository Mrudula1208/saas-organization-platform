import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantService, Tenant } from '../../../core/services/tenant';

interface Transaction {
  id: string;
  tenantName: string;
  plan: string;
  amount: number;
  date: string;
  status: string;
  invoiceId: string;
}

interface MonthlyRevenueRecord {
  month: string;
  amount: number;
  heightPercent: number;
}

@Component({
  selector: 'app-revenue',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './revenue.html',
  styleUrl: './revenue.css',
})
export class Revenue implements OnInit {
  mrr = 0;
  activeSubscribers = 0;
  arpu = 0;
  annualRecurringRevenue = 0;

  transactions: Transaction[] = [
    { id: '1', tenantName: 'Acme Corp', plan: 'Pro', amount: 45, date: '2026-05-25T10:30:00Z', status: 'Succeeded', invoiceId: 'INV-2026-042' },
    { id: '2', tenantName: 'Globex Corporation', plan: 'Enterprise', amount: 180, date: '2026-05-24T14:45:00Z', status: 'Succeeded', invoiceId: 'INV-2026-041' },
    { id: '3', tenantName: 'Umbrella Corp', plan: 'Pro', amount: 45, date: '2026-05-20T09:15:00Z', status: 'Succeeded', invoiceId: 'INV-2026-040' },
    { id: '4', tenantName: 'Initech Inc', plan: 'Basic', amount: 15, date: '2026-05-18T16:00:00Z', status: 'Succeeded', invoiceId: 'INV-2026-039' },
    { id: '5', tenantName: 'Acme Corp', plan: 'Pro', amount: 45, date: '2026-04-25T10:30:00Z', status: 'Succeeded', invoiceId: 'INV-2026-021' },
    { id: '6', tenantName: 'Globex Corporation', plan: 'Enterprise', amount: 180, date: '2026-04-24T14:45:00Z', status: 'Succeeded', invoiceId: 'INV-2026-020' }
  ];

  revenueHistory: MonthlyRevenueRecord[] = [
    { month: 'DEC', amount: 190, heightPercent: 35 },
    { month: 'JAN', amount: 240, heightPercent: 45 },
    { month: 'FEB', amount: 320, heightPercent: 60 },
    { month: 'MAR', amount: 380, heightPercent: 70 },
    { month: 'APR', amount: 440, heightPercent: 85 },
    { month: 'MAY', amount: 510, heightPercent: 100 }
  ];

  constructor(private tenantService: TenantService) {}

  ngOnInit() {
    this.calculateRevenueStats();
  }

  calculateRevenueStats() {
    this.tenantService.getAll().subscribe({
      next: (tenants: Tenant[]) => {
        this.activeSubscribers = tenants.length;
        
        // Sum the monthly revenue of all tenants
        this.mrr = tenants.reduce((sum: number, t: Tenant) => sum + (t.monthlyRevenue || 0), 0);
        this.arpu = this.activeSubscribers > 0 ? parseFloat((this.mrr / this.activeSubscribers).toFixed(2)) : 0;
        this.annualRecurringRevenue = this.mrr * 12;

        // Dynamically adjust the latest month (May) amount to reflect the live data
        const latestIdx = this.revenueHistory.findIndex((h: MonthlyRevenueRecord) => h.month === 'MAY');
        if (latestIdx !== -1) {
          this.revenueHistory[latestIdx].amount = this.mrr;
          
          // Recalculate all height percentages relative to the maximum monthly amount
          const maxAmt = Math.max(...this.revenueHistory.map((h: MonthlyRevenueRecord) => h.amount));
          if (maxAmt > 0) {
            this.revenueHistory.forEach((h: MonthlyRevenueRecord) => {
              h.heightPercent = Math.round((h.amount / maxAmt) * 100);
            });
          }
        }
      }
    });
  }
}

