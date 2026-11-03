import { Routes } from '@angular/router';
import { identifiedGuard, managerGuard } from './identity/identity.guards';
import { Who } from './identity/who';
import { Month } from './month/month';
import { Report } from './report/report';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'month' },
  { path: 'who', component: Who },
  { path: 'month', component: Month, canActivate: [identifiedGuard] },
  { path: 'report', component: Report, canActivate: [managerGuard] },
  { path: '**', redirectTo: 'month' },
];
