export interface WeightEntry {
  id: number;
  weightKg: number;
  weekNumber: number;
  year: number;
  weekLabel: string;
  recordedAt: string;
}

export interface CreateWeightEntryRequest {
  weightKg: number;
  weekNumber: number;
  year: number;
}

export interface UpdateWeightEntryRequest {
  weightKg: number;
}

export interface PendingWeightCheck {
  hasPendingEntry: boolean;
  pendingWeekNumber: number;
  pendingYear: number;
  pendingWeekLabel: string;
}
