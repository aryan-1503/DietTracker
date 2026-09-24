import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MonthlyReportMetaDto } from '../models/daily-intake.models';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/reports`;

  /** Fetch report metadata for a given month. */
  getMeta(year: number, month: number): Observable<MonthlyReportMetaDto> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get<MonthlyReportMetaDto>(`${this.base}/monthly/meta`, { params });
  }

  /** Download Excel file as Blob for a given month. */
  downloadExcel(year: number, month: number): Observable<Blob> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get(`${this.base}/monthly/download`, {
      params,
      responseType: 'blob',
    });
  }
}
