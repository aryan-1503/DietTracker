import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardDto } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/dashboard`;

  get(timeZoneId?: string): Observable<DashboardDto> {
    const tz = timeZoneId ?? Intl.DateTimeFormat().resolvedOptions().timeZone;
    const params = new HttpParams().set('tz', tz);
    return this.http.get<DashboardDto>(this.base, { params });
  }
}
