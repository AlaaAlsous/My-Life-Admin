import { Routes } from '@angular/router';

import { authGuard } from './auth/auth.guard';
import { DashboardPage } from './dashboard/dashboard-page';
import { LandingPage } from './landing/landing-page';

export const routes: Routes = [
  { path: '', component: LandingPage, title: 'My Life Admin' },
  { path: 'dashboard', component: DashboardPage, title: 'Dashboard', canActivate: [authGuard] },
];
