import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../shared/states/empty-state';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { DashboardService, DashboardSummary } from './dashboard.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, EmptyState, ErrorState, LoadingState, RouterLink],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly dashboardService = inject(DashboardService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly summary = signal<DashboardSummary | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
