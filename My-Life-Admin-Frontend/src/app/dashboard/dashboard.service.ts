import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DashboardDocument {
  id: number;
  title: string;
  category: string | null;
  uploadedAt: string;
  expiryDate: string | null;
}

export interface DashboardEvent {
  id: number;
  title: string;
  category: string;
  eventDate: string;
}

export interface DashboardReminder {
  id: number;
  reminderDate: string;
  title: string | null;
}

export interface DashboardSummary {
  recentDocuments: DashboardDocument[];
  upcomingEvents: DashboardEvent[];
  latestReminders: DashboardReminder[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/dashboard`;

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(this.url);
  }
}
