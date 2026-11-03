import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Employee } from '../api/employee';
import { EmployeeApi } from '../api/employee-api';
import { Identity } from './identity';

@Component({
  selector: 'app-who',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Who are you?</h1>
    <ul class="employees">
      @for (employee of employees(); track employee.id) {
        <li>
          <button type="button" (click)="choose(employee)">{{ employee.name }}</button>
        </li>
      }
    </ul>
  `,
  styles: `
    .employees {
      list-style: none;
      padding: 0;
      display: grid;
      gap: 0.5rem;
      max-width: 20rem;
    }
    button {
      width: 100%;
      padding: 0.6rem 1rem;
      font: inherit;
      cursor: pointer;
    }
  `,
})
export class Who {
  private readonly api = inject(EmployeeApi);
  private readonly identity = inject(Identity);
  private readonly router = inject(Router);

  protected readonly employees = toSignal(this.api.list(), { initialValue: [] });

  protected choose(employee: Employee): void {
    this.identity.select(employee);
    void this.router.navigate(['month']);
  }
}
