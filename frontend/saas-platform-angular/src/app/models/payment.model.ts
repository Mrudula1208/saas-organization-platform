export interface Payment {
  id: string;
  userId: string;
  tenantId: string;
  subscriptionPlanId: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  transactionId: string;
  paymentDate: string;
}

export interface PaymentRecord {
  id: string;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  transactionId: string;
}

export interface CurrentPlan {
  subscriptionPlanId: string;
  planName: string;
  price: number;
  maxUsers: number;
  maxProjects: number;
  storageLimitMB: number;
  billingFrequency: string;
  nextBillingDate: string | null;
}

export interface BillingSummary {
  totalPaid: number;
  totalPayments: number;
  successfulPayments: number;
  failedPayments: number;
  lastPaymentDate: string | null;
  currentPlan: CurrentPlan | null;
}