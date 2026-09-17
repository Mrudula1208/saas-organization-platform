import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface UserClaims {
  email: string;
  role: string;
  tenantId?: string;
  fullName?: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly apiUrl = environment.apiUrl + '/Auth';

  // Signal for active user state
  public currentUser = signal<UserClaims | null>(null);

  constructor(private http: HttpClient) {
    this.restoreSession();
  }

  // Decodes JWT payload locally
  private decodeToken(token: string): UserClaims | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));

      // Map standard JWT claims or custom claims
      return {
        email: payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || payload.Email || '',
        role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.Role || 'Member',
        tenantId: payload.tenantId || payload.TenantId || undefined,
        fullName: payload.fullName || payload.FullName || payload.name || 'User'
      };
    } catch (e) {
      return null;
    }
  }

  private restoreSession() {
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        const claims = this.decodeToken(token);
        if (claims) {
          this.currentUser.set(claims);
        } else {
          this.logout();
        }
      }
    }
  }

  login(dto: any): Observable<{ token: string; refreshToken?: string }> {
    return this.http.post<any>(`${this.apiUrl}/login`, dto).pipe(
      map((res) => {
        const token = res?.data?.accessToken || res?.token;
        if (!token) {
          throw new Error(res?.message || 'Login failed. Please check your credentials and try again.');
        }
        const refreshToken = res?.data?.refreshToken;
        return { token, refreshToken };
      }),
      tap((res) => this.saveToken(res.token, res.refreshToken))
    );
  }

  registerTenant(dto: any): Observable<{ token: string; refreshToken?: string }> {
    return this.http.post<any>(`${this.apiUrl}/register-tenant`, dto).pipe(
      map((res) => ({
        token: res?.data?.accessToken || res?.token || '',
        refreshToken: res?.data?.refreshToken
      })),
      tap((res) => {
        if (res.token) {
          this.saveToken(res.token, res.refreshToken);
        }
      })
    );
  }

  refreshToken(): Observable<{ token: string }> {
    const accessToken = typeof window !== 'undefined' ? localStorage.getItem('saas_token') : null;
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem('saas_refresh_token') : null;
    if (!accessToken || !refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }
    return this.http.post<any>(`${this.apiUrl}/refresh`, { accessToken, refreshToken }).pipe(
      map((res) => {
        const token = res?.data?.accessToken;
        const newRefresh = res?.data?.refreshToken;
        if (!token) {
          throw new Error('Failed to refresh authentication token.');
        }
        this.saveToken(token, newRefresh);
        return { token };
      })
    );
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(dto: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/reset-password`, dto);
  }

  private saveToken(token: string, refreshToken?: string) {
    if (typeof window !== 'undefined') {
      localStorage.setItem('saas_token', token);
      if (refreshToken) {
        localStorage.setItem('saas_refresh_token', refreshToken);
      }
      const claims = this.decodeToken(token);
      this.currentUser.set(claims);
    }
  }

  logout() {
    const token = typeof window !== 'undefined' ? localStorage.getItem('saas_token') : null;
    if (token) {
      // Best-effort server-side logout so the session end is audited; failures are ignored.
      this.http
        .post(`${this.apiUrl}/logout`, {}, { headers: { Authorization: `Bearer ${token}` } })
        .subscribe({ error: () => {} });
    }
    if (typeof window !== 'undefined') {
      localStorage.removeItem('saas_token');
      localStorage.removeItem('saas_refresh_token');
    }
    this.currentUser.set(null);
  }

  isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  hasRole(allowedRoles: string[]): boolean {
    const user = this.currentUser();
    if (!user) return false;
    return allowedRoles.includes(user.role);
  }

  getTenantId(): string | undefined {
    return this.currentUser()?.tenantId;
  }

  // Refresh cached claims after a profile update (e.g. new full name)
  updateCurrentUser(changes: Partial<UserClaims>) {
    const user = this.currentUser();
    if (user) {
      this.currentUser.set({ ...user, ...changes });
    }
  }
}
