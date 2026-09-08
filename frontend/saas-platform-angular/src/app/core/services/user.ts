import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Auth } from './auth';
import { User } from '../../models/user.model';
import { environment } from '../../../environments/environment';

export interface ApiResult {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly apiUrl = environment.apiUrl + '/User';

  constructor(private http: HttpClient, private auth: Auth) {}

  private getHeaders(): HttpHeaders {
    // Read local JWT token and attach Authorization header
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        return new HttpHeaders().set('Authorization', `Bearer ${token}`);
      }
    }
    return new HttpHeaders();
  }

  // ---------- Profile (authenticated user, identity from JWT) ----------

  getProfile(): Observable<User> {
    return this.http.get<any>(`${this.apiUrl}/profile`, { headers: this.getHeaders() }).pipe(
      map(res => res.data as User)
    );
  }

  updateProfile(fullName: string, profileImageUrl: string): Observable<ApiResult> {
    const payload = { fullName, profileImageUrl };
    return this.http.put<any>(`${this.apiUrl}/profile`, payload, { headers: this.getHeaders() }).pipe(
      map(res => ({ success: res?.success === true, message: res?.message || 'Profile updated successfully.' }))
    );
  }

  changePassword(currentPassword: string, newPassword: string, confirmPassword: string): Observable<ApiResult> {
    const payload = { currentPassword, newPassword, confirmPassword };
    return this.http.post<any>(`${this.apiUrl}/change-password`, payload, { headers: this.getHeaders() }).pipe(
      map(res => ({ success: res?.success === true, message: res?.message || 'Password changed successfully.' }))
    );
  }

  // ---------- User management (real API only) ----------
  // Errors are passed on to the calling page so real API failures are shown to the user.

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  getUserById(id: string): Observable<User> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res)
    );
  }

  createUser(dto: any): Observable<User> {
    const tenantId = this.auth.getTenantId() || dto.tenantId;

    // Structure expected by API
    const userPayload = {
      name: dto.fullName || dto.name,
      email: dto.email,
      password: dto.password,
      role: dto.role,
      tenantId: tenantId
    };

    return this.http.post<any>(this.apiUrl, userPayload, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res)
    );
  }

  updateUser(id: string, user: any): Observable<boolean> {
    return this.http.put(`${this.apiUrl}/${id}`, user, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  toggleStatus(id: string, isActive: boolean): Observable<boolean> {
    return this.http.post(`${this.apiUrl}/${id}/toggle-status`, { isActive }, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }

  deleteUser(id: string): Observable<boolean> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(() => true)
    );
  }
}
