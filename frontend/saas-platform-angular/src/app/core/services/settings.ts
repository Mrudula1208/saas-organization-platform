import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PlatformSettings, ApiResponse } from '../../models/platform-settings.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  private readonly apiUrl = environment.apiUrl + '/Settings';

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

  getSettings(): Observable<ApiResponse<PlatformSettings>> {
    return this.http.get<ApiResponse<PlatformSettings>>(this.apiUrl, {
      headers: this.getHeaders(),
    });
  }

  updateSettings(settings: PlatformSettings): Observable<ApiResponse<PlatformSettings>> {
    return this.http.put<ApiResponse<PlatformSettings>>(this.apiUrl, settings, {
      headers: this.getHeaders(),
    });
  }

  getPublicConfig(): Observable<{
    success: boolean;
    platformName: string;
    supportEmail: string;
    maintenanceMode: boolean;
    allowRegistrations: boolean;
    mfaRequired: boolean;
  }> {
    return this.http.get<any>(`${this.apiUrl}/public-config`);
  }
}
