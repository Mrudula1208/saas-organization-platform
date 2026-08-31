import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  BillingService,
  CurrentPlan,
  PaymentRecord,
  BillingSummary,
} from '../../../core/services/billing';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing.html',
  styleUrl: './billing.css',
})
export class Billing implements OnInit {
  currentPlan: CurrentPlan | null = null;
  summary: BillingSummary | null = null;
  payments: PaymentRecord[] = [];

  isLoading = true;
  errorMessage = '';

  constructor(private billingService: BillingService) {}

  ngOnInit() {
    this.loadBillingInfo();
  }

  loadBillingInfo() {
    this.isLoading = true;
    this.errorMessage = '';

    this.billingService.getBillingSummary().subscribe({
      next: (summary: BillingSummary) => {
        this.summary = summary;
        this.currentPlan = summary.currentPlan;
        this.loadPayments();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load billing information. Please try again later.';
      },
    });
  }

  private loadPayments() {
    this.billingService.getPayments().subscribe({
      next: (payments: PaymentRecord[]) => {
        this.payments = payments;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load payment history. Please try again later.';
      },
    });
  }

  get paymentMethod(): string {
    if (this.payments.length === 0) {
      return 'Not on file';
    }
    return this.payments[0].paymentMethod || 'Not on file';
  }

  isPaid(status: string): boolean {
    return status?.toLowerCase() === 'success' || status?.toLowerCase() === 'paid';
  }

  requestPlanChange() {
    alert(
      'Online payments are not configured in this environment. Please contact your platform administrator to change your subscription plan.'
    );
  }

  updatePaymentMethod() {
    alert(
      'Online payment methods are not configured in this environment. Please contact your platform administrator to update billing details.'
    );
  }

  downloadInvoice(id: string) {
    alert(
      `Invoice export is not configured in this environment. Reference: ${id}. Please contact your platform administrator.`
    );
  }
}