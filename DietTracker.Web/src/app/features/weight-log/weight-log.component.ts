import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { WeightService } from '../../services/weight.service';
import { WeightEntry, CreateWeightEntryRequest } from '../../models/weight.models';

@Component({
  selector: 'app-weight-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './weight-log.component.html',
  styleUrl: './weight-log.component.scss',
})
export class WeightLogComponent implements OnInit {
  private readonly weightSvc = inject(WeightService);
  private readonly router = inject(Router);

  readonly entries = signal<WeightEntry[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  // Add-new form
  readonly showAddForm = signal(false);
  addWeightKg: number | null = null;
  addWeekNumber: number | null = null;
  addYear: number = new Date().getFullYear();
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  // Inline edit state: keyed by entry id
  readonly editingId = signal<number | null>(null);
  editWeightKg: number | null = null;
  readonly editSaving = signal(false);
  readonly editError = signal<string | null>(null);

  // Delete confirmation
  readonly deletingId = signal<number | null>(null);

  get addValid(): boolean {
    if (this.addWeightKg === null || isNaN(this.addWeightKg)) return false;
    if (this.addWeightKg < 20 || this.addWeightKg > 300) return false;
    const str = this.addWeightKg.toString();
    const dot = str.indexOf('.');
    if (dot !== -1 && str.length - dot - 1 > 1) return false;
    if (!this.addWeekNumber || this.addWeekNumber < 1 || this.addWeekNumber > 53) return false;
    if (!this.addYear || this.addYear < 2000 || this.addYear > 2200) return false;
    return true;
  }

  get editValid(): boolean {
    if (this.editWeightKg === null || isNaN(this.editWeightKg)) return false;
    if (this.editWeightKg < 20 || this.editWeightKg > 300) return false;
    const str = this.editWeightKg.toString();
    const dot = str.indexOf('.');
    if (dot !== -1 && str.length - dot - 1 > 1) return false;
    return true;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.weightSvc.getAll().subscribe({
      next: entries => { this.entries.set(entries); this.loading.set(false); },
      error: () => { this.error.set('Failed to load weight entries.'); this.loading.set(false); },
    });
  }

  openAdd(): void {
    this.showAddForm.set(true);
    this.saveError.set(null);
    this.addWeightKg = null;
    this.addWeekNumber = null;
    this.addYear = new Date().getFullYear();
  }

  cancelAdd(): void {
    this.showAddForm.set(false);
    this.saveError.set(null);
  }

  submitAdd(): void {
    if (!this.addValid) return;
    this.saving.set(true);
    this.saveError.set(null);
    const req: CreateWeightEntryRequest = {
      weightKg: this.addWeightKg!,
      weekNumber: this.addWeekNumber!,
      year: this.addYear,
    };
    this.weightSvc.create(req).subscribe({
      next: () => {
        this.saving.set(false);
        this.showAddForm.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        if (err?.status === 409) {
          this.saveError.set('An entry for that week already exists.');
        } else {
          this.saveError.set('Failed to save. Please try again.');
        }
      },
    });
  }

  startEdit(entry: WeightEntry): void {
    this.editingId.set(entry.id);
    this.editWeightKg = entry.weightKg;
    this.editError.set(null);
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editWeightKg = null;
    this.editError.set(null);
  }

  submitEdit(entry: WeightEntry): void {
    if (!this.editValid) return;
    this.editSaving.set(true);
    this.editError.set(null);
    this.weightSvc.update(entry.id, { weightKg: this.editWeightKg! }).subscribe({
      next: () => {
        this.editSaving.set(false);
        this.editingId.set(null);
        this.load();
      },
      error: () => {
        this.editSaving.set(false);
        this.editError.set('Failed to update. Please try again.');
      },
    });
  }

  confirmDelete(id: number): void {
    this.deletingId.set(id);
  }

  cancelDelete(): void {
    this.deletingId.set(null);
  }

  submitDelete(id: number): void {
    this.weightSvc.delete(id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.load();
      },
      error: () => {
        this.deletingId.set(null);
        this.error.set('Failed to delete entry. Please try again.');
      },
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
