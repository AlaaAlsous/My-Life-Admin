import { Routes } from '@angular/router';

import { authGuard } from './auth/auth.guard';
import { DashboardPage } from './dashboard/dashboard-page';
import { DocumentViewerPage } from './documents/document-viewer-page';
import { DocumentsPage } from './documents/documents-page';
import { LandingPage } from './landing/landing-page';
import { EventComponent } from './events/event.component';
import { SettingsPage } from './settings/settings-page';

export const routes: Routes = [
  { path: '', component: LandingPage, title: 'My Life Admin' },
  { path: 'dashboard', component: DashboardPage, title: 'Dashboard', canActivate: [authGuard] },
  { path: 'documents', component: DocumentsPage, title: 'Documents', canActivate: [authGuard] },
  {
    path: 'documents/:id',
    component: DocumentViewerPage,
    title: 'Document',
    canActivate: [authGuard],
  },
  { path: 'events', component: EventComponent, title: 'Events', canActivate: [authGuard] },
  { path: 'settings', component: SettingsPage, title: 'Settings', canActivate: [authGuard] },
];
