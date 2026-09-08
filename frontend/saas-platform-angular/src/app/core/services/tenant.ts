import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Tenant, TenantSettings } from '../../models/tenant.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private readonly apiUrl = environment.apiUrl + '/Tenant';
  private readonly apiOrigin = environment.apiOrigin;

  // Converts a backend relative logo path into an absolute URL the browser can load.
  private toAbsoluteLogoUrl(url?: string): string | null {
    if (!url) return null;
    if (/^https?:\/\//i.test(url)) return url;
    return `${this.apiOrigin}${url.startsWith('/') ? url : `/${url}`}`;
  }

  constructor(private http: HttpClient) {}

  private mapPlanIdToName(planId: string): string {
    if (!planId) return 'Basic';
    const id = planId.toLowerCase();
    if (id === 'bbbb1111-2222-3333-4444-555566667777') return 'Basic';
    if (id === 'cccc1111-2222-3333-4444-555566667777') return 'Pro';
    if (id === 'eeee1111-2222-3333-4444-555566667777') return 'Enterprise';
    return 'Basic';
  }

  private mapPlanNameToId(name: string): string {
    if (!name) return 'bbbb1111-2222-3333-4444-555566667777';
    const n = name.toLowerCase();
    if (n === 'basic') return 'bbbb1111-2222-3333-4444-555566667777';
    if (n === 'pro') return 'cccc1111-2222-3333-4444-555566667777';
    if (n === 'enterprise') return 'eeee1111-2222-3333-4444-555566667777';
    return 'bbbb1111-2222-3333-4444-555566667777';
  }

  private mapBackendTenantToFrontend(t: any): Tenant {
    return {
      id: t.id,
      name: t.name,
      domain: t.domain,
      contactEmail: t.contactEmail || t.emailAddress || '',
      contactPhone: t.contactPhone || '',
      subscriptionPlanId: t.subscriptionPlanId || '',
      isActive: t.isActive === true,
      isDeleted: t.isDeleted === true,
      createdAt: t.createdAt,
      plan: t.plan || this.mapPlanIdToName(t.subscriptionPlanId),
      status: t.isActive ? 'Active' : 'Suspended',
      usersCount: t.usersCount || (t.users ? t.users.length : 0) || 0,
      projectsCount: t.projectsCount || (t.projects ? t.projects.length : 0) || 0,
      monthlyRevenue: t.monthlyRevenue || 0,
      logoImageUrl: this.toAbsoluteLogoUrl(t.logoImageUrl)
    };
  }

  private mapFrontendTenantToBackend(t: any): any {
    return {
      id: t.id,
      name: t.name,
      domain: t.domain,
      contactEmail: t.contactEmail || t.emailAddress || '',
      contactPhone: t.contactPhone || '',
      subscriptionPlanId: t.subscriptionPlanId || this.mapPlanNameToId(t.plan || 'Basic'),
      isActive: t.status ? (t.status === 'Active') : t.isActive === true
    };
  }

  getAll(): Observable<Tenant[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(tenants => tenants.map(t => this.mapBackendTenantToFrontend(t)))
    );
  }

  getById(id: string): Observable<Tenant> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(tenant => this.mapBackendTenantToFrontend(tenant))
    );
  }

  create(tenant: any): Observable<Tenant> {
    const backendPayload = {
      name: tenant.name,
      domain: tenant.domain || `${tenant.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`,
      contactEmail: tenant.contactEmail || tenant.emailAddress || '',
      contactPhone: tenant.contactPhone || '',
      subscriptionPlanId: this.mapPlanNameToId(tenant.plan || 'Basic'),
      isActive: true
    };

    return this.http.post<any>(this.apiUrl, backendPayload).pipe(
      map(res => this.mapBackendTenantToFrontend(res?.data || res))
    );
  }

  update(id: string, tenant: any): Observable<boolean> {
    const backendPayload = this.mapFrontendTenantToBackend({ ...tenant, id });

    return this.http.put(`${this.apiUrl}/${id}`, backendPayload).pipe(
      map(() => true)
    );
  }

  delete(id: string): Observable<boolean> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      map(() => true)
    );
  }

  // Uploads a logo image for a tenant. Returns the absolute logo URL served by the backend.
  uploadLogo(tenantId: string, file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<any>(`${this.apiUrl}/${tenantId}/upload-logo`, formData).pipe(
      map((res) => this.toAbsoluteLogoUrl(res?.logoUrl || '') || ''),
      catchError((err) => {
        const message = err?.error?.message || err?.message || 'Failed to upload logo. Please try again.';
        return throwError(() => new Error(message));
      })
    );
  }

  // Tenant settings. The backend derives the tenant id from the JWT, so no id is sent here.
  // These methods have no mock fallback: settings must come from / be saved to the real API.
  getSettings(): Observable<TenantSettings> {
    return this.http.get<any>(`${this.apiUrl}/settings`).pipe(
      map((res) => {
        const t = res?.data ?? res;
        return {
          id: t.id || '',
          name: t.name || '',
          domain: t.domain || '',
          contactEmail: t.contactEmail || '',
          contactPhone: t.contactPhone || '',
          logoImageUrl: this.toAbsoluteLogoUrl(t.logoImageUrl) || '',
          emailNotifications: t.emailNotifications !== false,
          inAppNotifications: t.inAppNotifications !== false
        } as TenantSettings;
      })
    );
  }

  updateSettings(payload: {
    name: string;
    contactEmail: string;
    contactPhone: string;
    emailNotifications: boolean;
    inAppNotifications: boolean;
  }): Observable<{ success: boolean; message: string }> {
    return this.http.put<any>(`${this.apiUrl}/settings`, payload).pipe(
      map((res) => ({
        success: res?.success !== false,
        message: res?.message || 'Workspace settings saved.'
      }))
    );
  }
}
