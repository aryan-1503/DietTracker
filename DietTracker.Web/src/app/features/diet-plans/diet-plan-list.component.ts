import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DietPlanService } from '../../services/diet-plan.service';
import { DietPlanSummaryDto } from '../../models/diet-plan.models';

@Component({
  selector: 'app-diet-plan-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="layout">
      <header class="topbar">
        <button class="icon-btn" (click)="goBack()" aria-label="Back">
          <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
        </button>
        <span class="topbar-title">My Diet Plans</span>
        <button class="icon-btn icon-btn--primary" (click)="createNew()" aria-label="Create plan">
          <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
        </button>
      </header>

      <main class="content">
        <div class="loader-wrap" *ngIf="loading()"><div class="loader"></div></div>

        <div *ngIf="!loading() && plans().length === 0" class="empty-state">
          <div class="empty-icon">🥗</div>
          <h2>No diet plans yet</h2>
          <p>Create your first plan to get started.</p>
          <button class="btn-primary" style="margin-top:1rem" (click)="createNew()">
            Create diet plan
          </button>
        </div>

        <div *ngIf="!loading() && plans().length > 0">
          <div *ngFor="let plan of plans()" class="plan-card" (click)="openPlan(plan.id)">

            <!-- Left: name + meta | Right: action buttons + primary label -->
            <div class="plan-header">
              <div class="plan-left">
                <span class="plan-name">{{ plan.name }}</span>
                <span class="plan-meta">{{ plan.mealSlotCount }} meal slot{{ plan.mealSlotCount !== 1 ? 's' : '' }}</span>
              </div>
              <div class="plan-right-col" (click)="$event.stopPropagation()">
                <div class="plan-actions">
                  <button class="icon-btn icon-btn--primary" (click)="editPlan(plan.id)" aria-label="Edit">
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                    </svg>
                  </button>
                  <button class="icon-btn icon-btn--danger" (click)="confirmDelete(plan)" aria-label="Delete">
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/>
                      <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
                    </svg>
                  </button>
                </div>
                <div class="plan-badge-row">
                  <span *ngIf="plan.isPrimary" class="primary-label">Primary</span>
                </div>
              </div>
            </div>

          </div>
        </div>
      </main>

      <!-- Delete confirmation overlay -->
      <div class="overlay" *ngIf="deletingPlan()" (click)="cancelDelete()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <h3>Delete "{{ deletingPlan()?.name }}"?</h3>
          <p>This will permanently remove the plan and all its meal slots and food options.</p>
          <div class="dialog-btns">
            <button class="btn-secondary" (click)="cancelDelete()">Cancel</button>
            <button class="btn-danger" (click)="doDelete()" [disabled]="deleteLoading()">
              {{ deleteLoading() ? 'Deleting…' : 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrl: './diet-plan-list.component.scss',
})
export class DietPlanListComponent implements OnInit {
  private svc = inject(DietPlanService);
  private router = inject(Router);

  readonly plans = signal<DietPlanSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly deletingPlan = signal<DietPlanSummaryDto | null>(null);
  readonly deleteLoading = signal(false);

  ngOnInit() { this.loadPlans(); }

  loadPlans() {
    this.loading.set(true);
    this.svc.getAll().subscribe({
      next: (p) => {
        // Primary plan always first
        this.plans.set([...p].sort((a, b) => (b.isPrimary ? 1 : 0) - (a.isPrimary ? 1 : 0)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  goBack()             { this.router.navigate(['/dashboard']); }
  createNew()          { this.router.navigate(['/diet-plans/new']); }
  openPlan(id: number) { this.router.navigate(['/diet-plans', id]); }
  editPlan(id: number) { this.router.navigate(['/diet-plans', id, 'edit']); }


  confirmDelete(plan: DietPlanSummaryDto) { this.deletingPlan.set(plan); }
  cancelDelete()                          { this.deletingPlan.set(null); }

  doDelete() {
    const plan = this.deletingPlan();
    if (!plan) return;
    this.deleteLoading.set(true);
    this.svc.delete(plan.id).subscribe({
      next: () => {
        this.plans.update(p => p.filter(x => x.id !== plan.id));
        this.deletingPlan.set(null);
        this.deleteLoading.set(false);
      },
      error: () => this.deleteLoading.set(false),
    });
  }
}
