import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { SubscriptionPlan } from '../../models/subscription.model';

@Injectable({
  providedIn: 'root',
})
export class SubscriptionPlanService {
  private readonly apiUrl = 'http://localhost:5258/api/SubscriptionPlan';

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

  getPlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  createPlan(data: {
    name: string;
    price: number;
    maxUsers: number;
    maxProjects: number;
    storageLimitMB: number;
  }): Observable<SubscriptionPlan> {
    return this.http.post<SubscriptionPlan>(this.apiUrl, data, { headers: this.getHeaders() });
  }

  updatePlan(
    id: string,
    data: {
      name: string;
      price: number;
      maxUsers: number;
      maxProjects: number;
      storageLimitMB: number;
      isActive: boolean;
    }
  ): Observable<boolean> {
    return this.http
      .put(`${this.apiUrl}/${id}`, data, { headers: this.getHeaders() })
      .pipe(map(() => true));
  }

  deletePlan(id: string): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() })
      .pipe(map(() => true));
  }
}