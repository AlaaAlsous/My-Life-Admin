import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  const url = 'http://localhost:5076/api/dashboard';

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  it('shows a loading state while fetching', () => {
    const { el, http } = setup();
    expect(el.querySelector('[role="status"]')).not.toBeNull();
    http.expectOne(url);
  });

  it('renders documents, events and reminders', async () => {
    const { fixture, el, http } = setup();
    http.expectOne(url).flush({
      recentDocuments: [
        {
          id: 1,
          title: 'Passport',
          category: 'ID',
          uploadedAt: '2026-06-01T00:00:00Z',
          expiryDate: null,
        },
      ],
      upcomingEvents: [
        { id: 2, title: 'Dentist', category: 'Health', eventDate: '2026-07-01T10:00:00Z' },
      ],
      latestReminders: [{ id: 3, reminderDate: '2026-06-10T00:00:00Z', title: 'Renew insurance' }],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const text = el.textContent ?? '';
    expect(text).toContain('Passport');
    expect(text).toContain('Dentist');
    expect(text).toContain('Renew insurance');
  });

  it('links documents, events and reminders to their pages', async () => {
    const { fixture, el, http } = setup();
    http.expectOne(url).flush({
      recentDocuments: [
        {
          id: 1,
          title: 'Passport',
          category: 'ID',
          uploadedAt: '2026-06-01T00:00:00Z',
          expiryDate: null,
        },
      ],
      upcomingEvents: [
        { id: 2, title: 'Dentist', category: 'Health', eventDate: '2026-07-01T10:00:00Z' },
      ],
      latestReminders: [{ id: 3, reminderDate: '2026-06-10T00:00:00Z', title: 'Renew insurance' }],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/documents/1');
    expect(hrefs.filter((href) => href === '/events').length).toBe(2);
  });

  it('links a reminder with an eventId to the event details', async () => {
    const { fixture, el, http } = setup();
    http.expectOne(url).flush({
      recentDocuments: [],
      upcomingEvents: [],
      latestReminders: [
        {
          id: 3,
          reminderDate: '2026-06-10T00:00:00Z',
          title: 'Renew insurance',
          eventId: 2,
          documentId: null,
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/events?event=2');
  });

  it('shows empty states when there is no data', async () => {
    const { fixture, el, http } = setup();
    http.expectOne(url).flush({ recentDocuments: [], upcomingEvents: [], latestReminders: [] });
    await fixture.whenStable();
    fixture.detectChanges();

    const text = el.textContent ?? '';
    expect(text).toContain('No documents yet');
    expect(text).toContain('No upcoming events');
    expect(text).toContain('No reminders');
  });

  it('shows an error and retries', async () => {
    const { fixture, el, http } = setup();
    http.expectOne(url).flush('fail', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')).not.toBeNull();
    el.querySelector<HTMLButtonElement>('[role="alert"] button')?.click();
    http.expectOne(url);
  });
});
