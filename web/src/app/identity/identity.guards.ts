import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Identity } from './identity';

export const identifiedGuard: CanActivateFn = () => {
  const identity = inject(Identity);
  return identity.current() ? true : inject(Router).createUrlTree(['who']);
};
