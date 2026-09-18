import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DailyIntakeService } from '../../services/daily-intake.service';
import { UpdateUserSettingsRequest } from '../../models/daily-intake.models';

// Common IANA timezone list (covers most users; shown as display label → IANA id)
const COMMON_TIMEZONES: { label: string; value: string }[] = [
  { label: 'UTC',                         value: 'UTC' },
  { label: 'India Standard Time (IST)',   value: 'Asia/Kolkata' },
  { label: 'US Eastern (ET)',             value: 'America/New_York' },
  { label: 'US Central (CT)',             value: 'America/Chicago' },
  { label: 'US Mountain (MT)',            value: 'America/Denver' },
  { label: 'US Pacific (PT)',             value: 'America/Los_Angeles' },
  { label: 'UK / Ireland (GMT/BST)',      value: 'Europe/London' },
  { label: 'Central European (CET)',      value: 'Europe/Berlin' },
  { label: 'Eastern European (EET)',      value: 'Europe/Helsinki' },
  { label: 'Moscow (MSK)',                value: 'Europe/Moscow' },
  { label: 'Gulf Standard Time (GST)',    value: 'Asia/Dubai' },
  { label: 'Pakistan Standard (PKT)',     value: 'Asia/Karachi' },
  { label: 'Bangladesh (BST)',            value: 'Asia/Dhaka' },
  { label: 'China / Singapore (CST/SGT)', value: 'Asia/Singapore' },
  { label: 'Japan / Korea (JST/KST)',     value: 'Asia/Tokyo' },
  { label: 'Australia Eastern (AEST)',    value: 'Australia/Sydney' },
  { label: 'Australia Western (AWST)',    value: 'Australia/Perth' },
  { label: 'New Zealand (NZST)',          value: 'Pacific/Auckland' },
  { label: 'Brazil (BRT)',                value: 'America/Sao_Paulo' },
  { label: 'Argentina (ART)',             value: 'America/Argentina/Buenos_Aires' },
];

@Component({
  selector: 'app-user-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-settings.component.html',
  styleUrl: './user-settings.component.scss',
})
export class UserSettingsComponent implements OnInit {
  private readonly svc = inject(DailyIntakeService);
  private readonly router = inject(Router);

  readonly timezones = COMMON_TIMEZONES;

  readonly loading = signal(true);
  readonly saving  = signal(false);
  readonly success = signal(false);
  readonly error   = signal<string | null>(null);

  reminderTime = '21:00';
  timeZoneId   = 'UTC';

  ngOnInit(): void {
    this.svc.getSettings().subscribe({
      next: s => {
        this.reminderTime = s.reminderTime;
        this.timeZoneId   = s.timeZoneId;
        // Ensure the stored TZ is in our list; add it as-is if not
        if (!COMMON_TIMEZONES.some(t => t.value === s.timeZoneId)) {
          this.timezones.push({ label: s.timeZoneId, value: s.timeZoneId });
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load settings.');
        this.loading.set(false);
      },
    });
  }

  save(): void {
    this.saving.set(true);
    this.success.set(false);
    this.error.set(null);

    const dto: UpdateUserSettingsRequest = {
      reminderTime: this.reminderTime,
      timeZoneId:   this.timeZoneId,
    };

    this.svc.updateSettings(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set(true);
        setTimeout(() => this.success.set(false), 3000);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save settings. Please try again.');
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
