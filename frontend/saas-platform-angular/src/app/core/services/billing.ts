import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

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

export interface PaymentRecord {
  id: string;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  transactionId: string;
}

export interface BillingSummary {
  totalPaid: number;
  totalPayments: number;
  successfulPayments: number;
  failedPayments: number;
  lastPaymentDate: string | null;
  currentPlan: CurrentPlan | null;
}

@Injectable({
  providedIn: 'root',
})
export class BillingService {
  private readonly apiUrl = 'https://localhost:7134/api/Billing';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        return new HttpHeaders().set('Authorization', `Bearer ${token}`);
      }
    }
    return new HttpHeaders();
  }

  getCurrentPlan(): Observable<CurrentPlan> {
    return this.http.get<CurrentPlan>(`${this.apiUrl}/current-plan`, {
      headers: this.getHeaders(),
    });
  }

  getPayments(): Observable<PaymentRecord[]> {
    return this.http.get<PaymentRecord[]>(`${this.apiUrl}/payments`, {
      headers: this.getHeaders(),
    });
  }

  getPayment(id: string): Observable<PaymentRecord> {
    return this.http.get<PaymentRecord>(`${this.apiUrl}/payments/${id}`, {
      headers: this.getHeaders(),
    });
  }

  getBillingSummary(): Observable<BillingSummary> {
    return this.http.get<BillingSummary>(`${this.apiUrl}/summary`, {
      headers: this.getHeaders(),
    });
  }
}