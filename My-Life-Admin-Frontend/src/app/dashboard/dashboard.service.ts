import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

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
  dueReminders: DashboardReminder[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:5076/api/dashboard';

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(this.url);
  }
}
