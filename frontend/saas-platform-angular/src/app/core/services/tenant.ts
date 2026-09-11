import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

export interface Tenant {
  id: string;
  name: string;
  domain: string;
  emailAddress: string;
  plan: string;
  status: string;
  createdAt: string;
  usersCount: number;
  projectsCount: number;
  monthlyRevenue: number;
  lastUsed: string;
}

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private readonly apiUrl = 'https://localhost:7134/api/Tenant';

  // Local state cache for mock database operations
  private mockTenants: Tenant[] = [
    {
      id: '11112222-3333-4444-5555-666677778888',
      name: 'Acme Corp',
      domain: 'acme.saasapp.com',
      emailAddress: 'admin@acme.com',
      plan: 'Pro',
      status: 'Upgraded',
      createdAt: '2025-10-15T08:30:00Z',
      usersCount: 54,
      projectsCount: 112,
      monthlyRevenue: 180,
      lastUsed: '7 months ago'
    },
    {
      id: '99998888-7777-6666-5555-444433332222',
      name: 'Globex Corporation',
      domain: 'globex.saasapp.com',
      emailAddress: 'homer@globex.com',
      plan: 'Enterprise',
      status: 'Upgraded',
      createdAt: '2025-11-20T10:00:00Z',
      usersCount: 120,
      projectsCount: 230,
      monthlyRevenue: 500,
      lastUsed: '1 month ago'
    },
    {
      id: '12345678-1234-1234-1234-123456789012',
      name: 'Initech Inc',
      domain: 'initech.saasapp.com',
      emailAddress: 'peter@initech.com',
      plan: 'Basic',
      status: 'Unintentended nomplete', // matches PDF spelling screenshot
      createdAt: '2026-01-05T14:45:00Z',
      usersCount: 12,
      projectsCount: 15,
      monthlyRevenue: 15,
      lastUsed: '2 months ago'
    },
    {
      id: '87654321-4321-4321-4321-210987654321',
      name: 'Umbrella Corp',
      domain: 'umbrella.saasapp.com',
      emailAddress: 'albert@umbrella.com',
      plan: 'Pro',
      status: 'Red upgraded', // matches PDF spelling screenshot
      createdAt: '2025-08-12T09:15:00Z',
      usersCount: 88,
      projectsCount: 140,
      monthlyRevenue: 180,
      lastUsed: '10 days ago'
    }
  ];

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
      emailAddress: t.contactEmail || t.emailAddress || '',
      plan: t.plan || this.mapPlanIdToName(t.subscriptionPlanId),
      status: t.isActive ? 'Active' : 'Suspended',
      createdAt: t.createdAt,
      usersCount: t.usersCount || (t.users ? t.users.length : 0) || 0,
      projectsCount: t.projectsCount || (t.projects ? t.projects.length : 0) || 0,
      monthlyRevenue: t.monthlyRevenue || 0,
      lastUsed: t.lastUsed || 'Recently'
    };
  }

  private mapFrontendTenantToBackend(t: any): any {
    return {
      id: t.id,
      name: t.name,
      domain: t.domain,
      contactEmail: t.emailAddress || t.contactEmail || '',
      contactPhone: t.contactPhone || '',
      subscriptionPlanId: this.mapPlanNameToId(t.plan || 'Basic'),
      isActive: t.status === 'Active' || t.isActive === true
    };
  }

  getAll(): Observable<Tenant[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(tenants => tenants.map(t => this.mapBackendTenantToFrontend(t))),
      catchError(() => {
        console.warn('Tenant API offline. Using mock tenants list.');
        return of([...this.mockTenants]);
      })
    );
  }

  getById(id: string): Observable<Tenant | null> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(tenant => this.mapBackendTenantToFrontend(tenant)),
      catchError(() => {
        console.warn(`Tenant API offline. Searching mock tenants for ID: ${id}`);
        const found = this.mockTenants.find(t => t.id === id) || null;
        return of(found);
      })
    );
  }

  create(tenant: any): Observable<Tenant> {
    const frontendTenant: Tenant = {
      id: tenant.id || crypto.randomUUID(),
      name: tenant.name,
      domain: tenant.domain || `${tenant.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`,
      emailAddress: tenant.emailAddress || tenant.adminEmail || '',
      plan: tenant.plan || 'Basic',
      status: tenant.status || 'Active',
      createdAt: tenant.createdAt || new Date().toISOString(),
      usersCount: tenant.usersCount || 1,
      projectsCount: tenant.projectsCount || 0,
      monthlyRevenue: tenant.plan === 'Enterprise' ? 180 : tenant.plan === 'Pro' ? 45 : 15,
      lastUsed: 'Just now'
    };

    const backendPayload = this.mapFrontendTenantToBackend(frontendTenant);

    return this.http.post<any>(this.apiUrl, backendPayload).pipe(
      map(res => this.mapBackendTenantToFrontend(res)),
      tap((res) => {
        this.mockTenants.push(res);
      }),
      catchError(() => {
        console.warn('Tenant API post failed. Saving tenant to local mock cache.');
        this.mockTenants.push(frontendTenant);
        return of(frontendTenant);
      })
    );
  }

  update(id: string, tenant: any): Observable<boolean> {
    const backendPayload = this.mapFrontendTenantToBackend({ ...tenant, id });

    return this.http.put(`${this.apiUrl}/${id}`, backendPayload).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Tenant API update failed. Updating mock tenant cache for ID: ${id}`);
        const idx = this.mockTenants.findIndex(t => t.id === id);
        if (idx !== -1) {
          this.mockTenants[idx] = { ...this.mockTenants[idx], ...tenant };
          return of(true);
        }
        return of(false);
      })
    );
  }

  delete(id: string): Observable<boolean> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`Tenant API delete failed. Removing from mock tenant cache for ID: ${id}`);
        const initialLength = this.mockTenants.length;
        this.mockTenants = this.mockTenants.filter(t => t.id !== id);
        return of(this.mockTenants.length < initialLength);
      })
    );
  }
}
