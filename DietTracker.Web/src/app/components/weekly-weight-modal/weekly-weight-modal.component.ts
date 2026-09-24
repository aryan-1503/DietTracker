import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WeightService } from '../../services/weight.service';
import { PendingWeightCheck } from '../../models/weight.models';

@Component({
  selector: 'app-weekly-weight-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './weekly-weight-modal.component.html',
  styleUrl: './weekly-weight-modal.component.scss',
})
export class WeeklyWeightModalComponent implements OnInit {
  @Input() pendingCheck!: PendingWeightCheck;
  /** Emitted when the user saves successfully — passes the saved entry. */
  @Output() saved = new EventEmitter<void>();
  /** Emitted when the user clicks "Enter Later". */
  @Output() dismissed = new EventEmitter<void>();

  weightInput: number | null = null;
  readonly saving = signal(false);
  readonly errorMsg = signal<string | null>(null);

  constructor(private weightSvc: WeightService) {}

  ngOnInit(): void {}

  get isValid(): boolean {
    if (this.weightInput === null || isNaN(this.weightInput)) return false;
    if (this.weightInput < 20 || this.weightInput > 300) return false;
    // Max 1 decimal place
    const str = this.weightInput.toString();
    const dotIdx = str.indexOf('.');
    if (dotIdx !== -1 && str.length - dotIdx - 1 > 1) return false;
    return true;
  }

  save(): void {
    if (!this.isValid) return;
    this.saving.set(true);
    this.errorMsg.set(null);

    this.weightSvc
      .create({
        weightKg: this.weightInput!,
        weekNumber: this.pendingCheck.pendingWeekNumber,
        year: this.pendingCheck.pendingYear,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.emit();
        },
        error: (err) => {
          this.saving.set(false);
          if (err?.status === 409) {
            this.errorMsg.set(
              'A weight entry for this week already exists.'
            );
          } else {
            this.errorMsg.set('Failed to save. Please try again.');
          }
        },
      });
  }

  dismiss(): void {
    this.dismissed.emit();
  }
}
