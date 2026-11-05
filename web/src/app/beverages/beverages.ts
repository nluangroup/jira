import { ChangeDetectionStrategy, Component, ElementRef, input, output, signal, viewChild } from '@angular/core';

const MAX = 999;

@Component({
  selector: 'app-beverages',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label for="beverages-count">Beverages</label>
    <button type="button" aria-label="Fewer beverages" [disabled]="disabled()" (click)="step(-1)">&minus;</button>
    <input
      #field
      id="beverages-count"
      type="number"
      min="0"
      [max]="max"
      step="1"
      [value]="value()"
      [disabled]="disabled()"
      (change)="change($event)"
    />
    <button type="button" aria-label="More beverages" [disabled]="disabled()" (click)="step(1)">+</button>
    @if (invalid()) {
      <p role="alert">Enter a whole number from 0 to 999.</p>
    }
  `,
})
export class Beverages {
  /** Saved beverage count for the month. */
  readonly value = input(0);
  /** Disables editing. */
  readonly disabled = input(false);
  /** Emits a valid new count. */
  readonly save = output<number>();

  readonly max = MAX;
  readonly invalid = signal(false);
  private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');

  /** Shows `count` in the field and clears any validation message, e.g. after a failed save or a month change. */
  reset(count: number): void {
    this.invalid.set(false);
    this.field().nativeElement.value = String(count);
  }

  change(event: Event): void {
    const raw = (event.target as HTMLInputElement).value.trim();
    const count = raw === '' ? NaN : Number(raw);
    if (!Number.isInteger(count) || count < 0 || count > MAX) {
      this.invalid.set(true);
      return;
    }
    this.invalid.set(false);
    this.save.emit(count);
  }

  step(delta: number): void {
    const next = Math.min(MAX, Math.max(0, this.value() + delta));
    this.invalid.set(false);
    if (next !== this.value()) this.save.emit(next);
  }
}
