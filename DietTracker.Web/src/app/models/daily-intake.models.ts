import { FoodOptionDto } from './diet-plan.models';

// ── Response models ───────────────────────────────────────────────────────────

export interface DailyEntryDto {
  id: number;
  userId: number;
  entryDate: string;       // YYYY-MM-DD
  dietPlanId: number;
  mealSlotId: number;
  followedPlan: boolean;
  actualTime: string | null;
  foodOptionId: number | null;
  otherText: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SlotIntakeDto {
  mealSlotId: number;
  mealCategory: string;
  startTime: string;
  endTime: string;
  sortOrder: number;
  isComplete: boolean;
  foodOptions: FoodOptionDto[];
  entry: DailyEntryDto | null;
}

export interface DailyIntakeDto {
  dietPlanId: number;
  dietPlanName: string;
  date: string;            // YYYY-MM-DD
  slots: SlotIntakeDto[];
}

export interface DailyIntakeResponse {
  hasPrimaryPlan: boolean;
  data: DailyIntakeDto | null;
}

// ── Request models ────────────────────────────────────────────────────────────

export interface UpsertDailyEntryRequest {
  entryDate: string;        // YYYY-MM-DD
  mealSlotId: number;
  followedPlan: boolean;
  actualTime: string | null;
  foodOptionId: number | null;
  otherText: string | null;
  addOtherToMealOptions: boolean;
}

// ── User Settings ─────────────────────────────────────────────────────────────

export interface UserSettingsDto {
  reminderTime: string;   // HH:mm
  timeZoneId: string;
}

export interface UpdateUserSettingsRequest {
  reminderTime: string;
  timeZoneId: string;
}
