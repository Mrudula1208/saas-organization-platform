import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SystemLog {
  id: string;
  action: string;
  description: string;
  userId?: string;
  tenantId?: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class SystemLogService {
  private readonly apiUrl = 'https://localhost:7134/api/SystemLog';

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

  getLogs(actionType?: string, startDate?: string, endDate?: string): Observable<SystemLog[]> {
    let params = new HttpParams();
    if (actionType) {
      params = params.set('actionType', actionType);
    }
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }
    return this.http.get<SystemLog[]>(this.apiUrl, { headers: this.getHeaders(), params });
  }
}