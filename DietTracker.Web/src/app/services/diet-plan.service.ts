import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
  CreateDietPlanRequest,
  DietPlanDto,
  DietPlanSummaryDto,
  ReorderSlotsRequest,
  UpdateDietPlanRequest,
} from '../models/diet-plan.models';

@Injectable({ providedIn: 'root' })
export class DietPlanService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/diet-plans`;

  getAll() {
    return this.http.get<DietPlanSummaryDto[]>(this.base);
  }

  getById(id: number) {
    return this.http.get<DietPlanDto>(`${this.base}/${id}`);
  }

  create(data: CreateDietPlanRequest) {
    return this.http.post<DietPlanDto>(this.base, data);
  }

  update(id: number, data: UpdateDietPlanRequest) {
    return this.http.put<DietPlanDto>(`${this.base}/${id}`, data);
  }

  setPrimary(id: number) {
    return this.http.put<{ message: string }>(`${this.base}/${id}/set-primary`, {});
  }

  reorderSlots(id: number, data: ReorderSlotsRequest) {
    return this.http.put<{ message: string }>(`${this.base}/${id}/slots/reorder`, data);
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
