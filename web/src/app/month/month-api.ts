import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface MonthState {
  month: string;
  lunchDays: string[];
  beverages: number;
}

@Injectable({ providedIn: 'root' })
export class MonthApi {
  private readonly http = inject(HttpClient);

  get(month: string): Observable<MonthState> {
    return this.http.get<MonthState>(`/api/me/months/${month}`);
  }

  tick(date: string): Observable<void> {
    return this.http.put<void>(`/api/me/lunch-days/${date}`, null);
  }

  untick(date: string): Observable<void> {
    return this.http.delete<void>(`/api/me/lunch-days/${date}`);
  }

  setBeverages(month: string, count: number): Observable<void> {
    return this.http.put<void>(`/api/me/months/${month}/beverages`, { count });
  }
}
