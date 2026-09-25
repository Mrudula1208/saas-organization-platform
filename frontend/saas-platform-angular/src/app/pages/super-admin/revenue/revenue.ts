import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantService } from '../../../core/services/tenant';
import { BillingService } from '../../../core/services/billing';
import { Tenant } from '../../../models/tenant.model';
import { AdminTransaction } from '../../../models/payment.model';
import { getErrorMessage } from '../../../core/helpers';

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

  transactions: AdminTransaction[] = [];
  revenueHistory: MonthlyRevenueRecord[] = [];

  isLoading = true;
  errorMessage = '';

  constructor(
    private tenantService: TenantService,
    private billingService: BillingService
  ) {}

  ngOnInit() {
    this.loadRevenueData();
  }

  loadRevenueData() {
    this.isLoading = true;
    this.errorMessage = '';

    // Load active tenants to calculate current MRR and ARPU
    this.tenantService.getAll(1, 200).subscribe({
      next: (res) => {
        const tenants = res.data;
        this.activeSubscribers = res.totalCount;

        // Sum monthly revenue across all tenant accounts
        this.mrr = tenants.reduce((sum: number, t: Tenant) => sum + (t.monthlyRevenue || 0), 0);
        this.arpu = this.activeSubscribers > 0 ? parseFloat((this.mrr / this.activeSubscribers).toFixed(2)) : 0;
        this.annualRecurringRevenue = this.mrr * 12;

        // Load database transactions for the ledger table and monthly chart
        this.billingService.getAdminTransactions().subscribe({
          next: (txs) => {
            this.transactions = txs;
            this.buildMonthlyHistory(txs, this.mrr);
            this.isLoading = false;
          },
          error: (err) => {
            // Still build history with MRR if transactions fail
            this.buildMonthlyHistory([], this.mrr);
            this.errorMessage = getErrorMessage(err, 'Could not load transaction history.');
            this.isLoading = false;
          }
        });
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Could not load revenue statistics.');
        this.isLoading = false;
      }
    });
  }

  private buildMonthlyHistory(txs: AdminTransaction[], currentMrr: number) {
    const monthNames = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
    const now = new Date();
    const history: MonthlyRevenueRecord[] = [];

    // Build the last 6 months progression
    for (let i = 5; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const mName = monthNames[d.getMonth()];
      const year = d.getFullYear();
      const monthIdx = d.getMonth();

      // Sum all successful payments made in this month and year
      const monthTotal = txs
        .filter(t => {
          const tDate = new Date(t.date);
          return tDate.getFullYear() === year &&
                 tDate.getMonth() === monthIdx &&
                 (t.status || '').toLowerCase() === 'success';
        })
        .reduce((sum, t) => sum + (t.amount || 0), 0);

      // If current month has no logged transaction receipts yet, use active MRR
      const finalAmount = (i === 0 && monthTotal === 0) ? currentMrr : monthTotal;

      history.push({
        month: mName,
        amount: finalAmount,
        heightPercent: 10
      });
    }

    // Scale bar heights relative to the highest earning month
    const maxAmt = Math.max(...history.map(h => h.amount), 1);
    history.forEach(h => {
      h.heightPercent = Math.max(10, Math.round((h.amount / maxAmt) * 100));
    });

    this.revenueHistory = history;
  }
}
