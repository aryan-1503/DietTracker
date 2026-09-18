import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DietPlanService } from '../../services/diet-plan.service';

// ── Validators ────────────────────────────────────────────────────────────────
function hhMm(ctrl: AbstractControl) {
  const v: string = ctrl.value ?? '';
  return /^([01]\d|2[0-3]):[0-5]\d$/.test(v) ? null : { hhMm: true };
}

function endAfterStart(group: AbstractControl) {
  const start: string = group.get('startTime')?.value ?? '';
  const end: string   = group.get('endTime')?.value   ?? '';
  if (!start || !end) return null;
  return start < end ? null : { endBeforeStart: true };
}

@Component({
  selector: 'app-diet-plan-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="layout">
      <header class="topbar">
        <button class="icon-btn" (click)="cancel()" aria-label="Cancel">
          <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
        </button>
        <span class="topbar-title">{{ isEdit() ? 'Edit Plan' : 'New Diet Plan' }}</span>
        <div class="topbar-actions"></div>
      </header>

      <div class="loader-wrap" *ngIf="loadingPlan()"><div class="loader"></div></div>

      <main class="content" *ngIf="!loadingPlan()" [formGroup]="form">

        <div *ngIf="serverError()" class="error-banner">{{ serverError() }}</div>

        <!-- Plan name -->
        <div class="card">
          <div class="field">
            <label for="planName">Plan name <span class="req">*</span></label>
            <input id="planName" type="text" formControlName="name"
                   [class.invalid]="isInvalid(form.get('name')!)"
                   placeholder="e.g. Weekday Diet Plan" />
            <span *ngIf="isInvalid(form.get('name')!)" class="field-error">Plan name is required.</span>
          </div>

          <!-- Primary checkbox -->
          <label class="primary-checkbox-row">
            <input type="checkbox" formControlName="isPrimary" />
            <span class="primary-checkbox-label">
              Set as primary plan
              <span class="primary-checkbox-hint">Your primary plan will be used for daily intake tracking</span>
            </span>
          </label>

          <!-- Start date -->
          <div class="field" style="margin-top:1rem">
            <label for="startDate">Start date</label>
            <input id="startDate" type="date" formControlName="startDate" />
            <span class="field-hint">When did you start following this plan? (optional)</span>
          </div>
        </div>

        <!-- Meal slots -->
        <div formArrayName="mealSlots">
          <div *ngFor="let slot of mealSlots.controls; let si = index" [formGroupName]="si">
            <div class="slot-card">
              <div class="slot-header">
                <span class="slot-num">Meal {{ si + 1 }}</span>
                <div class="slot-order-btns">
                  <button type="button" class="icon-btn" [disabled]="si === 0"
                          (click)="moveSlot(si, -1)" aria-label="Move up">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="18 15 12 9 6 15"/>
                    </svg>
                  </button>
                  <button type="button" class="icon-btn" [disabled]="si === mealSlots.length - 1"
                          (click)="moveSlot(si, 1)" aria-label="Move down">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="6 9 12 15 18 9"/>
                    </svg>
                  </button>
                  <button type="button" class="icon-btn icon-btn--danger"
                          [disabled]="mealSlots.length === 1"
                          (click)="removeSlot(si)" aria-label="Remove slot">
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Time row -->
              <div class="time-row">
                <div class="field">
                  <label>Start time <span class="req">*</span></label>
                  <input type="time" formControlName="startTime"
                         [class.invalid]="isInvalid(slot.get('startTime')!)" />
                  <span *ngIf="isInvalid(slot.get('startTime')!)" class="field-error">Required.</span>
                </div>
                <div class="field">
                  <label>End time <span class="req">*</span></label>
                  <input type="time" formControlName="endTime"
                         [class.invalid]="isInvalid(slot.get('endTime')!)" />
                  <span *ngIf="isInvalid(slot.get('endTime')!)" class="field-error">Required.</span>
                </div>
              </div>
              <span *ngIf="slot.errors?.['endBeforeStart'] && slot.touched"
                    class="field-error" style="margin-top:-0.5rem;margin-bottom:0.75rem;display:block;">
                End time must be after start time.
              </span>

              <!-- Meal category -->
              <div class="field">
                <label>Meal category <span class="req">*</span></label>
                <input type="text" formControlName="mealCategory"
                       [class.invalid]="isInvalid(slot.get('mealCategory')!)"
                       placeholder="e.g. Breakfast" />
                <span *ngIf="isInvalid(slot.get('mealCategory')!)" class="field-error">Required.</span>
              </div>

              <!-- Food options -->
              <p class="food-options-label">Food options <span class="req">*</span></p>
              <div formArrayName="foodOptions">
                <div *ngFor="let food of getFoods(si).controls; let fi = index"
                     [formGroupName]="fi" class="food-row">
                  <input type="text" formControlName="name"
                         [class.invalid]="isInvalid(food.get('name')!)"
                         [placeholder]="'Option ' + (fi + 1)" />
                  <button type="button" class="icon-btn icon-btn--danger"
                          [disabled]="getFoods(si).length === 1"
                          (click)="removeFood(si, fi)" aria-label="Remove food option">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                    </svg>
                  </button>
                </div>
                <span *ngIf="hasFoodError(si)" class="field-error">All food option fields are required.</span>
              </div>

              <button type="button" class="btn-ghost-add" style="margin-top:0.5rem" (click)="addFood(si)">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
                     fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                </svg>
                Add food option
              </button>
            </div>
          </div>
        </div>

        <!-- Add slot button -->
        <button type="button" class="btn-ghost-add" style="margin-bottom:1.25rem" (click)="addSlot()">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Add meal slot
        </button>

        <button type="button" class="btn-primary" (click)="submit()" [disabled]="saving()">
          {{ saving() ? 'Saving…' : (isEdit() ? 'Save changes' : 'Create plan') }}
        </button>

      </main>
    </div>
  `,
  styleUrl: './diet-plan-form.component.scss',
})
export class DietPlanFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private svc = inject(DietPlanService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly isEdit = signal(false);
  readonly loadingPlan = signal(false);
  readonly saving = signal(false);
  readonly serverError = signal('');

  private planId: number | null = null;

  form = this.fb.group({
    name: ['', Validators.required],
    isPrimary: [false],
    startDate: [''],   // YYYY-MM-DD string; empty = null
    mealSlots: this.fb.array([this.makeSlot()]),
  });

  get mealSlots() { return this.form.get('mealSlots') as FormArray; }
  getFoods(si: number) { return (this.mealSlots.at(si) as FormGroup).get('foodOptions') as FormArray; }

  hasFoodError(si: number): boolean {
    return this.getFoods(si).controls.some(c => c.get('name')!.invalid && c.get('name')!.touched);
  }

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.planId = +idParam;
      this.loadExisting(this.planId);
    }
  }

  loadExisting(id: number) {
    this.loadingPlan.set(true);
    this.svc.getById(id).subscribe({
      next: (plan) => {
        const slotsArray = this.fb.array([] as FormGroup[]);
        for (const slot of plan.mealSlots) {
          const sg = this.makeSlot(slot.startTime, slot.endTime, slot.mealCategory);
          const foods = sg.get('foodOptions') as FormArray;
          foods.clear();
          for (const fo of slot.foodOptions) {
            foods.push(this.makeFood(fo.name));
          }
          slotsArray.push(sg);
        }
        this.form.setControl('name', this.fb.control(plan.name, Validators.required));
        this.form.setControl('isPrimary', this.fb.control(plan.isPrimary));
        this.form.setControl('startDate', this.fb.control(plan.startDate ?? ''));
        this.form.setControl('mealSlots', slotsArray);
        this.loadingPlan.set(false);
      },
      error: () => { this.loadingPlan.set(false); this.router.navigate(['/diet-plans']); },
    });
  }

  isInvalid(ctrl: AbstractControl) { return ctrl.invalid && ctrl.touched; }

  makeSlot(startTime = '', endTime = '', mealCategory = ''): FormGroup {
    return this.fb.group({
      startTime:    [startTime,    [Validators.required, hhMm]],
      endTime:      [endTime,      [Validators.required, hhMm]],
      mealCategory: [mealCategory, Validators.required],
      foodOptions:  this.fb.array([this.makeFood()]),
    }, { validators: endAfterStart });
  }

  makeFood(name = ''): FormGroup {
    return this.fb.group({ name: [name, Validators.required] });
  }

  addSlot() { this.mealSlots.push(this.makeSlot()); }

  removeSlot(i: number) {
    if (this.mealSlots.length > 1) this.mealSlots.removeAt(i);
  }

  moveSlot(i: number, dir: -1 | 1) {
    const j = i + dir;
    if (j < 0 || j >= this.mealSlots.length) return;
    const a = this.mealSlots.at(i);
    const b = this.mealSlots.at(j);
    this.mealSlots.setControl(i, b);
    this.mealSlots.setControl(j, a);
  }

  addFood(si: number)              { this.getFoods(si).push(this.makeFood()); }
  removeFood(si: number, fi: number) {
    const foods = this.getFoods(si);
    if (foods.length > 1) foods.removeAt(fi);
  }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.saving.set(true);
    this.serverError.set('');

    const v = this.form.getRawValue() as any;
    const payload = {
      name: (v.name as string).trim(),
      isPrimary: v.isPrimary as boolean,
      startDate: v.startDate ? (v.startDate as string) : null,
      mealSlots: (v.mealSlots as any[]).map((s: any, i: number) => ({
        startTime: s.startTime,
        endTime: s.endTime,
        mealCategory: (s.mealCategory as string).trim(),
        sortOrder: i,
        foodOptions: (s.foodOptions as any[]).map((f: any, j: number) => ({
          name: (f.name as string).trim(),
          sortOrder: j,
        })),
      })),
    };

    const call = this.isEdit()
      ? this.svc.update(this.planId!, payload)
      : this.svc.create(payload);

    call.subscribe({
      next: (plan) => this.router.navigate(['/diet-plans', plan.id]),
      error: (err) => {
        this.serverError.set(err.error?.message ?? 'Failed to save. Please try again.');
        this.saving.set(false);
      },
    });
  }

  cancel() {
    if (this.isEdit()) this.router.navigate(['/diet-plans', this.planId]);
    else this.router.navigate(['/diet-plans']);
  }
}
