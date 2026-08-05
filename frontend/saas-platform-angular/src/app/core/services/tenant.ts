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

  getAll(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(this.apiUrl).pipe(
      catchError(() => {
        console.warn('Tenant API offline. Using mock tenants list.');
        return of([...this.mockTenants]);
      })
    );
  }

  getById(id: string): Observable<Tenant | null> {
    return this.http.get<Tenant>(`${this.apiUrl}/${id}`).pipe(
      catchError(() => {
        console.warn(`Tenant API offline. Searching mock tenants for ID: ${id}`);
        const found = this.mockTenants.find(t => t.id === id) || null;
        return of(found);
      })
    );
  }

  create(tenant: any): Observable<Tenant> {
    // Backend expects specific entities, so mapping
    const newTenant: Tenant = {
      id: crypto.randomUUID(),
      name: tenant.name,
      domain: tenant.domain || `${tenant.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`,
      emailAddress: tenant.emailAddress || tenant.adminEmail || '',
      plan: tenant.plan || 'Basic',
      status: 'Upgraded',
      createdAt: new Date().toISOString(),
      usersCount: 1,
      projectsCount: 0,
      monthlyRevenue: tenant.plan === 'Enterprise' ? 500 : tenant.plan === 'Pro' ? 180 : 15,
      lastUsed: 'Just now'
    };

    return this.http.post<Tenant>(this.apiUrl, newTenant).pipe(
      tap((res) => {
        this.mockTenants.push(res);
      }),
      catchError(() => {
        console.warn('Tenant API post failed. Saving tenant to local mock cache.');
        this.mockTenants.push(newTenant);
        return of(newTenant);
      })
    );
  }

  update(id: string, tenant: any): Observable<boolean> {
    return this.http.put(`${this.apiUrl}/${id}`, tenant).pipe(
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
