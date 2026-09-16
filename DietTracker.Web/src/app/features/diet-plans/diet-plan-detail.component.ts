import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { DietPlanService } from '../../services/diet-plan.service';
import { DietPlanDto } from '../../models/diet-plan.models';

@Component({
  selector: 'app-diet-plan-detail',
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
        <span class="topbar-title">{{ plan()?.name || 'Diet Plan' }}</span>
        <button class="icon-btn icon-btn--primary" (click)="edit()" aria-label="Edit plan">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
        </button>
      </header>

      <div class="loader-wrap" *ngIf="loading()"><div class="loader"></div></div>

      <main class="content" *ngIf="!loading() && plan() as p">
        <div *ngFor="let slot of p.mealSlots" class="slot-card">
          <div class="slot-header">
            <span class="slot-time">{{ slot.startTime }} – {{ slot.endTime }}</span>
            <span class="slot-category">{{ slot.mealCategory }}</span>
          </div>
          <ul class="food-list">
            <li *ngFor="let food of slot.foodOptions" class="food-item">
              <span class="food-dot"></span>
              {{ food.name }}
            </li>
          </ul>
        </div>
      </main>
    </div>
  `,
  styleUrl: './diet-plan-detail.component.scss',
})
export class DietPlanDetailComponent implements OnInit {
  private svc = inject(DietPlanService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly plan = signal<DietPlanDto | null>(null);
  readonly loading = signal(true);

  ngOnInit() {
    const id = +(this.route.snapshot.paramMap.get('id') ?? '0');
    this.svc.getById(id).subscribe({
      next: (p) => { this.plan.set(p); this.loading.set(false); },
      error: () => { this.loading.set(false); this.router.navigate(['/diet-plans']); },
    });
  }

  goBack() { this.router.navigate(['/diet-plans']); }
  edit()   { this.router.navigate(['/diet-plans', this.plan()!.id, 'edit']); }
}
