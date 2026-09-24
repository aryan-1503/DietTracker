import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  WeightEntry,
  CreateWeightEntryRequest,
  UpdateWeightEntryRequest,
  PendingWeightCheck,
} from '../models/weight.models';

@Injectable({ providedIn: 'root' })
export class WeightService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/weight-entries`;

  getAll(): Observable<WeightEntry[]> {
    return this.http.get<WeightEntry[]>(this.base);
  }

  getPendingCheck(tz?: string): Observable<PendingWeightCheck> {
    const timezone = tz ?? Intl.DateTimeFormat().resolvedOptions().timeZone;
    const params = new HttpParams().set('tz', timezone);
    return this.http.get<PendingWeightCheck>(`${this.base}/pending-check`, { params });
  }

  create(data: CreateWeightEntryRequest): Observable<WeightEntry> {
    return this.http.post<WeightEntry>(this.base, data);
  }

  update(id: number, data: UpdateWeightEntryRequest): Observable<WeightEntry> {
    return this.http.put<WeightEntry>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
