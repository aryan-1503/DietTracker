// ── Response models ───────────────────────────────────────────────────────────

export interface FoodOptionDto {
  id: number;
  name: string;
  sortOrder: number;
}

export interface MealSlotDto {
  id: number;
  startTime: string;   // HH:mm
  endTime: string;     // HH:mm
  mealCategory: string;
  sortOrder: number;
  foodOptions: FoodOptionDto[];
}

export interface DietPlanDto {
  id: number;
  name: string;
  isActive: boolean;
  isPrimary: boolean;
  createdAt: string;
  updatedAt: string;
  mealSlots: MealSlotDto[];
}

export interface DietPlanSummaryDto {
  id: number;
  name: string;
  isActive: boolean;
  isPrimary: boolean;
  mealSlotCount: number;
  createdAt: string;
  updatedAt: string;
}

// ── Request / form models ────────────────────────────────────────────────────

export interface FoodOptionRequest {
  name: string;
  sortOrder: number;
}

export interface MealSlotRequest {
  startTime: string;
  endTime: string;
  mealCategory: string;
  sortOrder: number;
  foodOptions: FoodOptionRequest[];
}

export interface CreateDietPlanRequest {
  name: string;
  isPrimary: boolean;
  mealSlots: MealSlotRequest[];
}

export interface UpdateDietPlanRequest {
  name: string;
  isPrimary: boolean;
  mealSlots: MealSlotRequest[];
}

export interface ReorderSlotsRequest {
  slotIds: number[];
}

// ── Local form state (used inside the component, not sent directly) ───────────

export interface FoodOptionForm {
  name: string;
}

export interface MealSlotForm {
  startTime: string;
  endTime: string;
  mealCategory: string;
  foodOptions: FoodOptionForm[];
}

export interface DietPlanForm {
  name: string;
  mealSlots: MealSlotForm[];
}
