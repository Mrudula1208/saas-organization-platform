import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface Plan {
  id: string;
  name: string;
  price: number;
  maxUsers: number;
  maxProjects: number;
  storageLimit: number; // displayed in GB
  isActive: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class SubscriptionPlanService {
  private readonly apiUrl = 'https://localhost:7134/api/SubscriptionPlan';

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

  private mapPlan(server: any): Plan {
    return {
      id: server.id,
      name: server.name,
      price: server.price,
      maxUsers: server.maxUsers,
      maxProjects: server.maxProjects,
      storageLimit: Math.round((server.storageLimitMB / 1024)),
      isActive: server.isActive,
    };
  }

  getPlans(): Observable<Plan[]> {
    return this.http
      .get<any[]>(this.apiUrl, { headers: this.getHeaders() })
      .pipe(map((list) => list.map((p) => this.mapPlan(p))));
  }

  createPlan(data: {
    name: string;
    price: number;
    maxUsers: number;
    maxProjects: number;
    storageLimit: number;
  }): Observable<Plan> {
    const payload = {
      name: data.name,
      price: data.price,
      maxUsers: data.maxUsers,
      maxProjects: data.maxProjects,
      storageLimitMB: data.storageLimit * 1024,
    };
    return this.http
      .post<any>(this.apiUrl, payload, { headers: this.getHeaders() })
      .pipe(map((p) => this.mapPlan(p)));
  }

  updatePlan(
    id: string,
    data: {
      name: string;
      price: number;
      maxUsers: number;
      maxProjects: number;
      storageLimit: number;
      isActive: boolean;
    }
  ): Observable<boolean> {
    const payload = {
      name: data.name,
      price: data.price,
      maxUsers: data.maxUsers,
      maxProjects: data.maxProjects,
      storageLimitMB: data.storageLimit * 1024,
      isActive: data.isActive,
    };
    return this.http
      .put(`${this.apiUrl}/${id}`, payload, { headers: this.getHeaders() })
      .pipe(map(() => true));
  }

  deletePlan(id: string): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() })
      .pipe(map(() => true));
  }
}