import {
  Component, OnInit, signal, computed, inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DailyIntakeService } from '../../services/daily-intake.service';
import {
  DailyIntakeDto,
  SlotIntakeDto,
  UpsertDailyEntryRequest,
  UpsertDailyNoteRequest,
} from '../../models/daily-intake.models';

const OTHER_ID = -1;           // sentinel value for the "Other" option
const OTHER_MAX_CHARS = 500;   // configurable char limit for free-text

interface SlotFormState {
  slotId: number;
  followedPlan: boolean;
  actualTime: string;
  foodOptionId: number | null;  // -1 = Other
  otherText: string;
  addToMealOptions: boolean;
  saving: boolean;
  error: string | null;
}

@Component({
  selector: 'app-daily-intake',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './daily-intake.component.html',
  styleUrl: './daily-intake.component.scss',
})
export class DailyIntakeComponent implements OnInit {
  private readonly svc = inject(DailyIntakeService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly intake = signal<DailyIntakeDto | null>(null);
  readonly hasPrimaryPlan = signal(false);
  readonly selectedDate = signal(this.todayStr());
  readonly expandedSlotId = signal<number | null>(null);
  readonly formStates = signal<Map<number, SlotFormState>>(new Map());

  // ── Daily Note State ───────────────────────────────────────────────────────
  readonly dailyNote = signal<string>('');
  readonly dailyNoteSaving = signal(false);
  readonly dailyNoteError = signal<string | null>(null);
  readonly dailyNoteSaved = signal(false);
  readonly NOTE_MAX_CHARS = 1000;

  readonly OTHER_ID = OTHER_ID;
  readonly OTHER_MAX_CHARS = OTHER_MAX_CHARS;

  // ── Computed ───────────────────────────────────────────────────────────────
  readonly completedCount = computed(() =>
    this.intake()?.slots.filter(s => s.isComplete).length ?? 0);
  readonly totalCount = computed(() =>
    this.intake()?.slots.length ?? 0);
  readonly progressPct = computed(() =>
    this.totalCount() === 0 ? 0 : Math.round((this.completedCount() / this.totalCount()) * 100));
  readonly allComplete = computed(() =>
    this.totalCount() > 0 && this.completedCount() === this.totalCount());
  readonly isToday = computed(() => this.selectedDate() === this.todayStr());
  readonly noteCharsLeft = computed(() => this.NOTE_MAX_CHARS - this.dailyNote().length);

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    // Support ?date=YYYY-MM-DD from dashboard recent-activity links
    const dateParam = this.route.snapshot.queryParamMap.get('date');
    if (dateParam && /^\d{4}-\d{2}-\d{2}$/.test(dateParam)) {
      this.selectedDate.set(dateParam);
    }
    this.loadIntake();
  }

  private loadIntake(): void {
    this.loading.set(true);
    this.error.set(null);
    this.expandedSlotId.set(null);
    this.formStates.set(new Map());

    const obs = this.isToday()
      ? this.svc.getToday()
      : this.svc.getByDate(this.selectedDate());

    obs.subscribe({
      next: res => {
        this.hasPrimaryPlan.set(res.hasPrimaryPlan);
        this.intake.set(res.data);
        this.dailyNote.set(res.data?.dailyNote?.noteText ?? '');
        this.dailyNoteError.set(null);
        this.dailyNoteSaved.set(false);
        this.loading.set(false);
        // Pre-expand first incomplete slot for today
        if (res.hasPrimaryPlan && res.data && this.isToday()) {
          const first = res.data.slots.find(s => !s.isComplete);
          if (first) this.openSlot(first);
        }
      },
      error: () => {
        this.error.set('Failed to load intake data. Please try again.');
        this.loading.set(false);
      },
    });
  }

  // ── Date navigation ────────────────────────────────────────────────────────
  goToPrevDay(): void {
    const d = new Date(this.selectedDate() + 'T00:00:00');
    d.setDate(d.getDate() - 1);
    this.selectedDate.set(this.dateToStr(d));
    this.loadIntake();
  }

  goToNextDay(): void {
    if (this.isToday()) return;
    const d = new Date(this.selectedDate() + 'T00:00:00');
    d.setDate(d.getDate() + 1);
    this.selectedDate.set(this.dateToStr(d));
    this.loadIntake();
  }

  goToToday(): void {
    if (this.isToday()) return;
    this.selectedDate.set(this.todayStr());
    this.loadIntake();
  }

  formattedDate(): string {
    const d = new Date(this.selectedDate() + 'T00:00:00');
    return d.toLocaleDateString(undefined, {
      weekday: 'short', month: 'short', day: 'numeric',
    });
  }

  // ── Slot expansion ─────────────────────────────────────────────────────────
  toggleSlot(slot: SlotIntakeDto): void {
    if (this.expandedSlotId() === slot.mealSlotId) {
      this.expandedSlotId.set(null);
    } else {
      this.openSlot(slot);
    }
  }

  private openSlot(slot: SlotIntakeDto): void {
    this.expandedSlotId.set(slot.mealSlotId);
    if (!this.getFormState(slot.mealSlotId)) {
      this.initFormState(slot);
    }
  }

  private initFormState(slot: SlotIntakeDto): void {
    const entry = slot.entry;
    const state: SlotFormState = {
      slotId: slot.mealSlotId,
      // Default to true (followed) for new entries; use saved value when one exists
      followedPlan: entry ? entry.followedPlan : true,
      actualTime: entry?.actualTime ?? '',
      foodOptionId: entry
        ? (entry.foodOptionId ?? (entry.otherText ? OTHER_ID : null))
        : null,
      otherText: entry?.otherText ?? '',
      addToMealOptions: false,
      saving: false,
      error: null,
    };
    const map = new Map(this.formStates());
    map.set(slot.mealSlotId, state);
    this.formStates.set(map);
  }

  getFormState(slotId: number): SlotFormState | undefined {
    return this.formStates().get(slotId);
  }

  updateFormState(slotId: number, patch: Partial<SlotFormState>): void {
    const map = new Map(this.formStates());
    const existing = map.get(slotId);
    if (existing) {
      if ('foodOptionId' in patch && patch.foodOptionId !== OTHER_ID) {
        // Clear "Other" fields when switching away from Other
        patch.otherText = '';
        patch.addToMealOptions = false;
      }
      map.set(slotId, { ...existing, ...patch });
      this.formStates.set(map);
    }
  }

  isOtherSelected(slotId: number): boolean {
    return this.getFormState(slotId)?.foodOptionId === OTHER_ID;
  }

  otherCharsLeft(slotId: number): number {
    return OTHER_MAX_CHARS - (this.getFormState(slotId)?.otherText.length ?? 0);
  }

  // ── Save ───────────────────────────────────────────────────────────────────
  saveEntry(slot: SlotIntakeDto): void {
    const state = this.getFormState(slot.mealSlotId);
    if (!state) return;

    // Validate
    if (state.followedPlan) {
      const hasFood = state.foodOptionId !== null && state.foodOptionId !== OTHER_ID;
      const hasOther = state.foodOptionId === OTHER_ID && state.otherText.trim().length > 0;
      if (!hasFood && !hasOther) {
        this.updateFormState(slot.mealSlotId, { error: 'Please select a food item or describe what you consumed.' });
        return;
      }
      if (state.foodOptionId === OTHER_ID && state.otherText.length > OTHER_MAX_CHARS) {
        this.updateFormState(slot.mealSlotId, { error: `Description must be ${OTHER_MAX_CHARS} characters or fewer.` });
        return;
      }
    }

    this.updateFormState(slot.mealSlotId, { saving: true, error: null });

    const isOther = state.foodOptionId === OTHER_ID;
    const payload: UpsertDailyEntryRequest = {
      entryDate: this.selectedDate(),
      mealSlotId: slot.mealSlotId,
      followedPlan: state.followedPlan,
      actualTime: state.actualTime || null,
      foodOptionId: state.followedPlan && !isOther ? state.foodOptionId : null,
      otherText: state.followedPlan && isOther ? state.otherText.trim() : null,
      addOtherToMealOptions: state.followedPlan && isOther && state.addToMealOptions,
    };

    this.svc.upsert(payload).subscribe({
      next: entry => {
        // Patch the local intake state immediately
        const current = this.intake();
        if (current) {
          const updatedSlots = current.slots.map(s =>
            s.mealSlotId === slot.mealSlotId
              ? { ...s, isComplete: true, entry }
              : s
          );
          // If "add to meal options" was checked, also append to local food options
          if (payload.addOtherToMealOptions && payload.otherText) {
            const idx = updatedSlots.findIndex(s => s.mealSlotId === slot.mealSlotId);
            if (idx !== -1 && !updatedSlots[idx].foodOptions.some(fo => fo.name === payload.otherText)) {
              const maxOrder = updatedSlots[idx].foodOptions.reduce((m, fo) => Math.max(m, fo.sortOrder), -1);
              updatedSlots[idx] = {
                ...updatedSlots[idx],
                foodOptions: [
                  ...updatedSlots[idx].foodOptions,
                  { id: 0, name: payload.otherText!, sortOrder: maxOrder + 1 },
                ],
              };
            }
          }
          this.intake.set({ ...current, slots: updatedSlots });
        }
        // Remove cached form state so re-opening re-initialises from the saved entry
        const map = new Map(this.formStates());
        map.delete(slot.mealSlotId);
        this.formStates.set(map);
        this.expandedSlotId.set(null);
      },
      error: () => {
        this.updateFormState(slot.mealSlotId, { saving: false, error: 'Failed to save. Please try again.' });
      },
    });
  }

  cancelEdit(slotId: number): void {
    this.expandedSlotId.set(null);
    const slot = this.intake()?.slots.find(s => s.mealSlotId === slotId);
    if (slot) this.initFormState(slot); // reset to saved state
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private todayStr(): string {
    return this.dateToStr(new Date());
  }

  private dateToStr(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  goToDietPlans(): void {
    this.router.navigate(['/diet-plans']);
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  // ── Daily Note ─────────────────────────────────────────────────────────────

  saveNote(): void {
    if (this.dailyNote().length > this.NOTE_MAX_CHARS) {
      this.dailyNoteError.set(`Note must be ${this.NOTE_MAX_CHARS} characters or fewer.`);
      return;
    }
    this.dailyNoteSaving.set(true);
    this.dailyNoteError.set(null);
    this.dailyNoteSaved.set(false);

    const payload: UpsertDailyNoteRequest = {
      entryDate: this.selectedDate(),
      noteText: this.dailyNote().trim() || null,
    };

    this.svc.upsertNote(payload).subscribe({
      next: () => {
        this.dailyNoteSaving.set(false);
        this.dailyNoteSaved.set(true);
        setTimeout(() => this.dailyNoteSaved.set(false), 2500);
      },
      error: () => {
        this.dailyNoteSaving.set(false);
        this.dailyNoteError.set('Failed to save note. Please try again.');
      },
    });
  }
}
