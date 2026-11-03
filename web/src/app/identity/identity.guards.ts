import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Identity } from './identity';

export const identifiedGuard: CanActivateFn = () => {
  const identity = inject(Identity);
  return identity.current() ? true : inject(Router).createUrlTree(['who']);
};

export const managerGuard: CanActivateFn = () => {
  const identity = inject(Identity);
  if (!identity.current()) {
    return inject(Router).createUrlTree(['who']);
  }
  return identity.isManager() ? true : inject(Router).createUrlTree(['month']);
};
