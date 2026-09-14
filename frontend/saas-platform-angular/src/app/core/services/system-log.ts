import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SystemLog } from '../../models/system-log.model';
import { PagedResult } from '../../models/paged-result.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SystemLogService {
  private readonly apiUrl = environment.apiUrl + '/SystemLog';

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

  // One page of logs, filtered and counted on the server.
  getLogs(actionType?: string, startDate?: string, endDate?: string, search?: string, page = 1, pageSize = 20): Observable<PagedResult<SystemLog>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (actionType) {
      params = params.set('actionType', actionType);
    }
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<SystemLog>>(this.apiUrl, { headers: this.getHeaders(), params });
  }
}