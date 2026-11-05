import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Identity } from '../identity/identity';

interface ReportRow {
  employeeId: string;
  name: string;
  lunchDays: number;
  beverages: number;
}

interface MonthReport {
  month: string;
  rows: ReportRow[];
  total: { lunchDays: number; beverages: number };
}

function currentMonth(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Copenhagen',
    year: 'numeric',
    month: '2-digit',
  }).format(new Date());
}

@Component({
  selector: 'app-report',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Report</h1>
    @if (!isManager()) {
      <p>This page is for managers only.</p>
    } @else {
      <label>
        Month
        <input type="month" [value]="month()" (input)="month.set($any($event.target).value)" />
      </label>
      <button type="button" (click)="exportCsv()" [disabled]="!month()">Export CSV</button>

      @if (report.error()) {
        <p role="alert">Could not load the report.</p>
      } @else if (report.value(); as data) {
        <table>
          <thead>
            <tr>
              <th scope="col">Employee</th>
              <th scope="col">Lunch days</th>
              <th scope="col">Beverages</th>
            </tr>
          </thead>
          <tbody>
            @for (row of data.rows; track row.employeeId) {
              <tr>
                <th scope="row">{{ row.name }}</th>
                <td>{{ row.lunchDays }}</td>
                <td>{{ row.beverages }}</td>
              </tr>
            }
            <tr>
              <th scope="row">Total</th>
              <td>{{ data.total.lunchDays }}</td>
              <td>{{ data.total.beverages }}</td>
            </tr>
          </tbody>
        </table>
      }
    }
  `,
})
export class Report {
  private readonly http = inject(HttpClient);
  protected readonly isManager = inject(Identity).isManager;
  protected readonly month = signal(currentMonth());

  protected readonly report = rxResource({
    params: computed(() => (this.isManager() && this.month() ? this.month() : undefined)),
    stream: ({ params }) => this.http.get<MonthReport>(`/api/reports/${params}`),
  });

  protected exportCsv(): void {
    const month = this.month();
    this.http.get(`/api/reports/${month}/csv`, { responseType: 'blob' }).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `lunch-${month}.csv`;
      link.click();
      URL.revokeObjectURL(url);
    });
  }
}
