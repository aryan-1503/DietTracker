import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  DailyIntakeResponse,
  DailyEntryDto,
  UpsertDailyEntryRequest,
  UserSettingsDto,
  UpdateUserSettingsRequest,
} from '../models/daily-intake.models';

@Injectable({ providedIn: 'root' })
export class DailyIntakeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/daily-entries`;
  private readonly settingsBase = `${environment.apiUrl}/settings`;

  /** Load today's primary-plan slots merged with entries. */
  getToday(): Observable<DailyIntakeResponse> {
    return this.http.get<DailyIntakeResponse>(`${this.base}/today`);
  }

  /** Load any date's primary-plan slots merged with entries (YYYY-MM-DD). */
  getByDate(date: string): Observable<DailyIntakeResponse> {
    const params = new HttpParams().set('date', date);
    return this.http.get<DailyIntakeResponse>(this.base, { params });
  }

  /** Create or update a daily entry. */
  upsert(dto: UpsertDailyEntryRequest): Observable<DailyEntryDto> {
    return this.http.post<DailyEntryDto>(this.base, dto);
  }

  /** Delete a daily entry by id. */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  // ── Settings ────────────────────────────────────────────────────────────────

  getSettings(): Observable<UserSettingsDto> {
    return this.http.get<UserSettingsDto>(this.settingsBase);
  }

  updateSettings(dto: UpdateUserSettingsRequest): Observable<UserSettingsDto> {
    return this.http.put<UserSettingsDto>(this.settingsBase, dto);
  }
}
