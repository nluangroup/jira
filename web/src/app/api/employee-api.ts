import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Employee } from './employee';

@Injectable({ providedIn: 'root' })
export class EmployeeApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Employee[]> {
    return this.http.get<Employee[]>('/api/employees');
  }
}
