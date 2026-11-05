import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { Beverages } from '../beverages/beverages';
import { MonthApi } from './month-api';

const copenhagenDate = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Europe/Copenhagen',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});
const monthName = new Intl.DateTimeFormat('en-US', { timeZone: 'UTC', month: 'long', year: 'numeric' });

const pad = (n: number) => String(n).padStart(2, '0');

interface DayCell {
  date: string;
  day: number;
  disabled: boolean;
}

@Component({
  selector: 'app-month',
  imports: [Beverages],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './month.html',
  styleUrl: './month.css',
})
export class Month {
  private readonly api = inject(MonthApi);
  private readonly today = copenhagenDate.format(Date.now());

  readonly weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  readonly month = signal(this.today.slice(0, 7));
  readonly lunchDays = signal<string[]>([]);
  readonly beverages = signal(0);
  readonly loadFailed = signal(false);
  readonly saveFailed = signal(false);

  readonly loaded = signal(false);
  readonly beveragesDisabled = computed(() => !this.loaded() || this.month() > this.today.slice(0, 7));
  private readonly spinner = viewChild(Beverages);

  readonly title = computed(() => {
    const [year, month] = this.month().split('-').map(Number);
    return monthName.format(new Date(Date.UTC(year, month - 1, 1)));
  });

  readonly leadingBlanks = computed(() => {
    const [year, month] = this.month().split('-').map(Number);
    return Array.from({ length: (new Date(Date.UTC(year, month - 1, 1)).getUTCDay() + 6) % 7 });
  });

  readonly days = computed<DayCell[]>(() => {
    const [year, month] = this.month().split('-').map(Number);
    const count = new Date(Date.UTC(year, month, 0)).getUTCDate();
    return Array.from({ length: count }, (_, i) => {
      const day = i + 1;
      const date = `${this.month()}-${pad(day)}`;
      const weekday = new Date(Date.UTC(year, month - 1, day)).getUTCDay();
      return { date, day, disabled: weekday === 0 || weekday === 6 || date > this.today };
    });
  });

  constructor() {
    this.load();
  }

  isTicked(date: string): boolean {
    return this.lunchDays().includes(date);
  }

  shift(delta: number): void {
    const [year, month] = this.month().split('-').map(Number);
    const shifted = new Date(Date.UTC(year, month - 1 + delta, 1));
    this.month.set(`${shifted.getUTCFullYear()}-${pad(shifted.getUTCMonth() + 1)}`);
    this.load();
  }

  toggle(date: string): void {
    const requested = this.month();
    const ticked = this.isTicked(date);
    const call = ticked ? this.api.untick(date) : this.api.tick(date);
    this.saveFailed.set(false);
    call.subscribe({
      next: () => {
        if (this.month() !== requested) return;
        this.lunchDays.update((days) => {
          const without = days.filter((d) => d !== date);
          return ticked ? without : [...without, date];
        });
      },
      error: () => {
        if (this.month() === requested) this.saveFailed.set(true);
      },
    });
  }

  saveBeverages(count: number): void {
    const requested = this.month();
    this.saveFailed.set(false);
    this.api.setBeverages(requested, count).subscribe({
      next: () => {
        if (this.month() === requested) this.beverages.set(count);
      },
      error: () => {
        if (this.month() !== requested) return;
        this.saveFailed.set(true);
        this.spinner()?.reset(this.beverages());
      },
    });
  }

  private load(): void {
    const requested = this.month();
    this.loadFailed.set(false);
    this.saveFailed.set(false);
    this.lunchDays.set([]);
    this.beverages.set(0);
    this.loaded.set(false);
    this.api.get(requested).subscribe({
      next: (state) => {
        if (this.month() !== requested) return;
        this.lunchDays.set(state.lunchDays);
        this.beverages.set(state.beverages);
        this.loaded.set(true);
        this.spinner()?.reset(state.beverages);
      },
      error: () => {
        if (this.month() === requested) this.loadFailed.set(true);
      },
    });
  }
}
