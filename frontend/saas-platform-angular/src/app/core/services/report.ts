import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TenantReportData, AdminReportData } from '../../models/report.model';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly apiUrl = 'http://localhost:5258/api/Reports';

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

  getTenantReport(): Observable<TenantReportData> {
    return this.http.get<TenantReportData>(`${this.apiUrl}/tenant-report`, {
      headers: this.getHeaders(),
    });
  }

  getAdminReport(): Observable<AdminReportData> {
    return this.http.get<AdminReportData>(`${this.apiUrl}/admin-report`, {
      headers: this.getHeaders(),
    });
  }
}