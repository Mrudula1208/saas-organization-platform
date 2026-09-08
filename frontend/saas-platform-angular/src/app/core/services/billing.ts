import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CurrentPlan, PaymentRecord, BillingSummary, Payment } from '../../models/payment.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class BillingService {
  private readonly apiUrl = environment.apiUrl + '/Billing';

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

  getPayment(id: string): Observable<Payment> {
    return this.http.get<Payment>(`${this.apiUrl}/payments/${id}`, {
      headers: this.getHeaders(),
    });
  }

  getBillingSummary(): Observable<BillingSummary> {
    return this.http.get<BillingSummary>(`${this.apiUrl}/summary`, {
      headers: this.getHeaders(),
    });
  }
}