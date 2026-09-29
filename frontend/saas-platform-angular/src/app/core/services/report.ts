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

  getAdminDashboard(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/admin-dashboard`, {
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

  exportAdminReportPdf(): Observable<{ blob: Blob; fileName: string }> {
    return this.http
      .get(`${this.apiUrl}/admin-report/export/pdf`, {
        headers: this.getHeaders(),
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(map((res) => this.toFileResponse(res, 'platform-analytics.pdf')));
  }

  exportAdminReportExcel(): Observable<{ blob: Blob; fileName: string }> {
    return this.http
      .get(`${this.apiUrl}/admin-report/export/excel`, {
        headers: this.getHeaders(),
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(map((res) => this.toFileResponse(res, 'platform-analytics.xlsx')));
  }

  getTenantExportUrl(format: 'PDF' | 'Excel'): string {
    const token = typeof window !== 'undefined' ? localStorage.getItem('saas_token') || '' : '';
    const endpoint = format === 'PDF' ? 'export/pdf' : 'export/excel';
    return `${this.apiUrl}/${endpoint}?token=${encodeURIComponent(token)}`;
  }

  getAdminExportUrl(format: 'PDF' | 'Excel'): string {
    const token = typeof window !== 'undefined' ? localStorage.getItem('saas_token') || '' : '';
    const endpoint = format === 'PDF' ? 'admin-report/export/pdf' : 'admin-report/export/excel';
    return `${this.apiUrl}/${endpoint}?token=${encodeURIComponent(token)}`;
  }

  private toFileResponse(
    res: HttpResponse<Blob>,
    fallbackName: string
  ): { blob: Blob; fileName: string } {
    const disposition = res.headers.get('Content-Disposition') || res.headers.get('content-disposition') || '';
    let extractedName = '';

    // Check RFC 5987 encoded filename: filename*=UTF-8''filename.ext
    const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
    if (utf8Match && utf8Match[1]) {
      try {
        extractedName = decodeURIComponent(utf8Match[1].trim());
      } catch {
        extractedName = utf8Match[1].trim();
      }
    } else {
      // Check standard: filename="filename.ext" or filename=filename.ext
      const match = /filename=["']?([^"';]+)["']?/i.exec(disposition);
      if (match && match[1]) {
        extractedName = match[1].trim();
      }
    }

    // Clean up any remaining quotes or illegal characters
    extractedName = extractedName.replace(/^["']|["']$/g, '').trim();

    // Verify valid extension matching fallback
    const expectedExt = fallbackName.slice(fallbackName.lastIndexOf('.')).toLowerCase();
    let finalFileName = extractedName || fallbackName;

    if (!finalFileName.toLowerCase().endsWith(expectedExt)) {
      finalFileName = finalFileName.includes('.')
        ? finalFileName.slice(0, finalFileName.lastIndexOf('.')) + expectedExt
        : finalFileName + expectedExt;
    }

    return {
      blob: res.body ?? new Blob(),
      fileName: finalFileName,
    };
  }
}
