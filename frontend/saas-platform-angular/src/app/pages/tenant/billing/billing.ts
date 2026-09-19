import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BillingService } from '../../../core/services/billing';
import { SubscriptionPlanService } from '../../../core/services/subscription-plan';
import {
  CurrentPlan,
  PaymentRecord,
  BillingSummary,
} from '../../../models/payment.model';
import { SubscriptionPlan } from '../../../models/subscription.model';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './billing.html',
  styleUrl: './billing.css',
})
export class Billing implements OnInit {
  currentPlan: CurrentPlan | null = null;
  summary: BillingSummary | null = null;
  payments: PaymentRecord[] = [];

  isLoading = true;
  errorMessage = '';

  // Upgrade Modal State
  isUpgradeModalOpen = false;
  availablePlans: SubscriptionPlan[] = [];
  selectedPlan: SubscriptionPlan | null = null;
  selectedPaymentMethod = 'Credit Card';
  isProcessingUpgrade = false;
  upgradeSuccessMessage = '';
  upgradeErrorMessage = '';

  // Update Card Modal State
  isUpdateCardModalOpen = false;
  cardForm = {
    cardholderName: '',
    cardNumber: '•••• •••• •••• 4242',
    expiry: '12/28',
    cvv: '•••'
  };

  constructor(
    private billingService: BillingService,
    private planService: SubscriptionPlanService
  ) {}

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
    this.upgradeErrorMessage = '';
    this.upgradeSuccessMessage = '';
    this.isUpgradeModalOpen = true;
    this.loadPlans();
  }

  loadPlans() {
    this.planService.getPlans().subscribe({
      next: (plans) => {
        this.availablePlans = plans.filter((p) => p.isActive);
        if (!this.selectedPlan && this.availablePlans.length > 0) {
          this.selectedPlan =
            this.availablePlans.find(
              (p) => p.id.toLowerCase() !== this.currentPlan?.subscriptionPlanId?.toLowerCase()
            ) || this.availablePlans[0];
        }
      },
      error: (err) => {
        this.upgradeErrorMessage = getErrorMessage(err, 'Failed to load subscription plans.');
      },
    });
  }

  selectPlan(plan: SubscriptionPlan) {
    if (plan.id.toLowerCase() === this.currentPlan?.subscriptionPlanId?.toLowerCase()) {
      return;
    }
    this.selectedPlan = plan;
  }

  closeUpgradeModal() {
    this.isUpgradeModalOpen = false;
    this.selectedPlan = null;
  }

  confirmUpgrade() {
    if (!this.selectedPlan) return;

    this.isProcessingUpgrade = true;
    this.upgradeErrorMessage = '';

    this.billingService
      .createPayment({
        subscriptionPlanId: this.selectedPlan.id,
        amount: this.selectedPlan.price,
        paymentMethod: this.selectedPaymentMethod,
      })
      .subscribe({
        next: () => {
          this.isProcessingUpgrade = false;
          this.isUpgradeModalOpen = false;
          this.upgradeSuccessMessage = `Workspace successfully upgraded to the ${this.selectedPlan?.name} plan!`;
          this.loadBillingInfo();
        },
        error: (err) => {
          this.isProcessingUpgrade = false;
          this.upgradeErrorMessage = getErrorMessage(
            err,
            'Failed to process payment upgrade. Please verify your details and try again.'
          );
        },
      });
  }

  updatePaymentMethod() {
    this.isUpdateCardModalOpen = true;
  }

  closeUpdateCardModal() {
    this.isUpdateCardModalOpen = false;
  }

  saveCard() {
    this.isUpdateCardModalOpen = false;
    this.upgradeSuccessMessage = 'Payment method details updated successfully.';
  }

  downloadInvoice(inv: PaymentRecord) {
    const invoiceWindow = window.open('', '_blank');
    if (!invoiceWindow) return;
    const invId = inv.transactionId || inv.id;
    const invDate = new Date(inv.paymentDate).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
    const invAmount = Number(inv.amount).toFixed(2);
    const method = inv.paymentMethod || 'Credit Card';

    const invoiceHtml = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>Invoice - ${invId}</title>
        <style>
          body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 40px; color: #1e293b; line-height: 1.5; }
          .header { display: flex; justify-content: space-between; border-bottom: 2px solid #e2e8f0; padding-bottom: 20px; }
          .title { font-size: 24px; font-weight: 800; color: #0f172a; }
          .badge { display: inline-block; padding: 4px 12px; background: #ecfdf5; color: #059669; border-radius: 9999px; font-size: 13px; font-weight: 700; }
          .details { margin: 30px 0; }
          .meta-table { width: 100%; margin-bottom: 25px; }
          .meta-table td { padding: 4px 0; }
          .table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          .table th, .table td { padding: 12px 16px; text-align: left; border-bottom: 1px solid #e2e8f0; }
          .table th { background: #f8fafc; font-weight: 600; color: #475569; }
          .total { text-align: right; font-size: 20px; font-weight: 800; margin-top: 25px; color: #0f172a; }
          @media print { .no-print { display: none; } }
        </style>
      </head>
      <body>
        <div class="header">
          <div>
            <div class="title">INVOICE RECEIPT</div>
            <p style="margin: 4px 0 0 0; color: #64748b; font-size: 14px;">Multi-Tenant SaaS Platform</p>
          </div>
          <div style="text-align: right;">
            <div class="badge">${inv.paymentStatus || 'Paid'}</div>
          </div>
        </div>
        <div class="details">
          <table class="meta-table">
            <tr>
              <td style="width: 50%;"><strong>Invoice ID:</strong> <code>${invId}</code></td>
              <td><strong>Invoice Date:</strong> ${invDate}</td>
            </tr>
            <tr>
              <td><strong>Billing Method:</strong> ${method}</td>
              <td><strong>Payment Status:</strong> ${inv.paymentStatus}</td>
            </tr>
          </table>
          <table class="table">
            <thead>
              <tr>
                <th>Description</th>
                <th>Billing Cycle</th>
                <th style="text-align: right;">Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Platform Subscription Fee</td>
                <td>Monthly</td>
                <td style="text-align: right; font-weight: 600;">$${invAmount}</td>
              </tr>
            </tbody>
          </table>
          <div class="total">Total Paid: $${invAmount} USD</div>
        </div>
        <div class="no-print" style="margin-top: 40px; text-align: center;">
          <button onclick="window.print()" style="padding: 10px 24px; background: #6366f1; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 15px; font-weight: 600;">Print / Save as PDF</button>
        </div>
      </body>
      </html>
    `;
    invoiceWindow.document.write(invoiceHtml);
    invoiceWindow.document.close();
  }
}