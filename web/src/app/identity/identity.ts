import { Injectable, computed, signal } from '@angular/core';
import { Employee } from '../api/employee';

const storageKey = 'lunch.employee';

@Injectable({ providedIn: 'root' })
export class Identity {
  private readonly selected = signal<Employee | null>(read());

  readonly current = this.selected.asReadonly();
  readonly isManager = computed(() => this.selected()?.isOfficeManager ?? false);

  select(employee: Employee): void {
    localStorage.setItem(storageKey, JSON.stringify(employee));
    this.selected.set(employee);
  }

  clear(): void {
    localStorage.removeItem(storageKey);
    this.selected.set(null);
  }
}

function read(): Employee | null {
  const raw = localStorage.getItem(storageKey);
  return raw ? (JSON.parse(raw) as Employee) : null;
}
