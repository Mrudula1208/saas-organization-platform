import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { TenantReportData, AdminReportData } from '../../models/report.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly apiUrl = environment.apiUrl + '/Reports';

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

  exportTenantReportPdf(): Observable<{ blob: Blob; fileName: string }> {
    return this.http
      .get(`${this.apiUrl}/export/pdf`, {
        headers: this.getHeaders(),
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(map((res) => this.toFileResponse(res, 'workspace-analytics.pdf')));
  }

  exportTenantReportExcel(): Observable<{ blob: Blob; fileName: string }> {
    return this.http
      .get(`${this.apiUrl}/export/excel`, {
        headers: this.getHeaders(),
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(map((res) => this.toFileResponse(res, 'workspace-analytics.xlsx')));
  }

  private toFileResponse(
    res: HttpResponse<Blob>,
    fallbackName: string
  ): { blob: Blob; fileName: string } {
    const disposition = res.headers.get('Content-Disposition') ?? '';
    const match = /filename="?([^";]+)"?/i.exec(disposition);
    return {
      blob: res.body ?? new Blob(),
      fileName: match?.[1] ?? fallbackName,
    };
  }
}
