import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export interface MonthlyStat {
  year: number;
  month: number;
  count: number;
}

export interface QuarterlyStat {
  year: number;
  quarter: number;
  count: number;
}

export interface TenantReportData {
  monthlyProjects: MonthlyStat[];
  monthlyTasks: MonthlyStat[];
  totalTasks: number;
  completedTasks: number;
  totalMembers: number;
  totalProjects: number;
  avgTasksPerMember: number;
  completionRate: number;
}

export interface AdminReportData {
  quarterlyTenants: QuarterlyStat[];
  monthlyUsers: MonthlyStat[];
  totalTenants: number;
  totalUsers: number;
  avgLifetimeMonths: number;
  customerAcquisitionCost: number;
  churnRate: number;
}

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly apiUrl = 'https://localhost:7134/api/Reports';

  constructor(private http: HttpClient) {}

  getTenantReport(): Observable<TenantReportData | null> {
    return this.http.get<TenantReportData>(`${this.apiUrl}/tenant-report`).pipe(
      map((res: any) => res),
      catchError(() => {
        console.warn('Tenant report API offline. Using fallback data.');
        return of(null);
      })
    );
  }

  getAdminReport(): Observable<AdminReportData | null> {
    return this.http.get<AdminReportData>(`${this.apiUrl}/admin-report`).pipe(
      map((res: any) => res),
      catchError(() => {
        console.warn('Admin report API offline. Using fallback data.');
        return of(null);
      })
    );
  }
}
