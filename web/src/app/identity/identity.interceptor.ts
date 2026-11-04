import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Identity } from './identity';

export const identityInterceptor: HttpInterceptorFn = (request, next) => {
  const identity = inject(Identity);
  const router = inject(Router);
  const employee = identity.current();
  const outgoing = employee
    ? request.clone({ setHeaders: { 'X-Employee-Id': employee.id } })
    : request;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        identity.clear();
        void router.navigate(['who']);
      }
      return throwError(() => error);
    }),
  );
};
