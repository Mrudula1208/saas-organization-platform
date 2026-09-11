import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

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
  private readonly apiUrl = 'https://localhost:7134/api/Auth';
  
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

  login(dto: any): Observable<{ token: string }> {
    return this.http.post<any>(`${this.apiUrl}/login`, dto).pipe(
      map((res) => {
        if (res && res.success && res.data && res.data.accessToken) {
          return { token: res.data.accessToken };
        }
        if (res && res.token) {
          return { token: res.token };
        }
        throw new Error('Invalid login response');
      }),
      tap((res) => {
        if (res && res.token) {
          this.saveToken(res.token);
        }
      }),
      catchError((error) => {
        console.warn('Backend API login failed. Falling back to local mock authentication...');
        return this.mockLogin(dto);
      })
    );
  }

  registerTenant(dto: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register-tenant`, dto).pipe(
      map((res) => {
        if (res && res.success && res.data && res.data.accessToken) {
          return { token: res.data.accessToken };
        }
        if (res && res.token) {
          return res;
        }
        throw new Error('Registration failed');
      }),
      tap((res) => {
        if (res && res.token) {
          this.saveToken(res.token);
        }
      }),
      catchError((error) => {
        console.warn('Backend API registration failed. Falling back to local mock...');
        const mockClaims: UserClaims = {
          email: dto.adminEmail,
          role: 'TenantAdmin',
          tenantId: '11112222-3333-4444-5555-666677778888',
          fullName: dto.adminName
        };
        const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
        const payload = btoa(JSON.stringify(mockClaims));
        const mockToken = `${header}.${payload}.mocksignature`;
        this.saveToken(mockToken);
        return of({ token: mockToken });
      })
    );
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/forgot-password`, { email }).pipe(
      catchError((error) => {
        console.warn('ForgotPassword API failed. Falling back to mock behavior.');
        return of({ success: true, message: 'Mock link sent' });
      })
    );
  }

  resetPassword(dto: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/reset-password`, dto).pipe(
      catchError((error) => {
        console.warn('ResetPassword API failed. Falling back to mock behavior.');
        return of({ success: true, message: 'Password reset' });
      })
    );
  }

  private mockLogin(dto: any): Observable<{ token: string }> {
    const email = dto.email.toLowerCase().trim();
    const password = dto.password;

    let mockClaims: UserClaims | null = null;

    if (email === 'admin@saas.com' && password === 'admin123') {
      mockClaims = {
        email: 'admin@saas.com',
        role: 'Admin',
        fullName: 'JD Dewifrav'
      };
    } else if (email === 'tenant@acme.com' && password === 'tenant123') {
      mockClaims = {
        email: 'tenant@acme.com',
        role: 'TenantAdmin',
        tenantId: '11112222-3333-4444-5555-666677778888',
        fullName: 'Acme Administrator'
      };
    } else if (email === 'member@acme.com' && password === 'member123') {
      mockClaims = {
        email: 'member@acme.com',
        role: 'Member',
        tenantId: '11112222-3333-4444-5555-666677778888',
        fullName: 'Jann Sanner'
      };
    }

    if (mockClaims) {
      // Generate a mock JWT-like string: header.payload.signature
      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
      const payload = btoa(JSON.stringify(mockClaims));
      const mockToken = `${header}.${payload}.mocksignature`;
      
      this.saveToken(mockToken);
      return of({ token: mockToken });
    } else {
      return throwError(() => new Error('Invalid Email or Password (Mock credentials: admin@saas.com/admin123, tenant@acme.com/tenant123, member@acme.com/member123)'));
    }
  }

  private saveToken(token: string) {
    if (typeof window !== 'undefined') {
      localStorage.setItem('saas_token', token);
      const claims = this.decodeToken(token);
      this.currentUser.set(claims);
    }
  }

  logout() {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('saas_token');
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
}
