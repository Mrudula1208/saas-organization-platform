import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Auth } from './auth';
import { User } from '../../models/user.model';

export interface ApiResult {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly apiUrl = 'http://localhost:5258/api/User';

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

  // ---------- User management (real API only - no mock data) ----------
  // Failures degrade to empty/false results (never fake records), so pages
  // that do not handle HTTP errors keep behaving gracefully.

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl, { headers: this.getHeaders() }).pipe(
      catchError(() => {
        console.warn('User API getUsers failed.');
        return of([] as User[]);
      })
    );
  }

  getUserById(id: string): Observable<User | null> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res),
      catchError(() => {
        console.warn(`User API getUserById failed for ${id}.`);
        return of(null);
      })
    );
  }

  createUser(dto: any): Observable<User> {
    const tenantId = this.auth.getTenantId() || dto.tenantId;

    // Structure expected by API
    const userPayload = {
      name: dto.fullName || dto.name,
      email: dto.email,
      password: dto.password,
      tenantId: tenantId
    };

    return this.http.post<any>(this.apiUrl, userPayload, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res),
      catchError(() => {
        console.warn('User API createUser failed.');
        return of(null as unknown as User);
      })
    );
  }

  updateUser(id: string, user: any): Observable<boolean> {
    return this.http.put(`${this.apiUrl}/${id}`, user, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`User API updateUser failed for ${id}.`);
        return of(false);
      })
    );
  }

  deleteUser(id: string): Observable<boolean> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`User API deleteUser failed for ${id}.`);
        return of(false);
      })
    );
  }
}
