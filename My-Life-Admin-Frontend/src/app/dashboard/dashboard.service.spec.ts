import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DashboardSummary, DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  const dashboardUrl = 'http://localhost:5076/api/dashboard';
  let service: DashboardService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the dashboard summary', () => {
    const summary: DashboardSummary = {
      recentDocuments: [
        {
          id: 1,
          title: 'Passport',
          category: 'Identity',
          uploadedAt: '2026-06-01T00:00:00Z',
          expiryDate: '2030-01-01',
        },
      ],
      upcomingEvents: [
        { id: 2, title: 'Dentist', category: 'Health', eventDate: '2026-07-01T10:00:00Z' },
      ],
      latestReminders: [
        {
          id: 3,
          reminderDate: '2026-06-10T00:00:00Z',
          title: 'Renew insurance',
          eventId: 2,
          documentId: null,
        },
      ],
    };
    let result: DashboardSummary | undefined;

    service.getSummary().subscribe((value) => {
      result = value;
    });

    const request = http.expectOne(dashboardUrl);
    expect(request.request.method).toBe('GET');
    request.flush(summary);

    expect(result).toEqual(summary);
  });
});
