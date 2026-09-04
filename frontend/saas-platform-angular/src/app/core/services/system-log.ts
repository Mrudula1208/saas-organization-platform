import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SystemLog } from '../../models/system-log.model';

@Injectable({
  providedIn: 'root',
})
export class SystemLogService {
  private readonly apiUrl = 'http://localhost:5258/api/SystemLog';

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