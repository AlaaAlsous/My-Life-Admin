import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EventDto } from '../models/event.dto';
import { EventComponent } from './event.component';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';

describe('EventComponent', () => {
  const eventsUrl = 'http://localhost:5076/api/events/';
  const remindersUrl = 'http://localhost:5076/api/reminders/';

  const event: EventDto = {
    id: 1,
    userId: 1,
    documentId: 7,
    title: 'Passport expires',
    description: 'Renew in time',
    category: 'Identity',
    eventDate: new Date().toISOString(),
    createdAt: '2026-01-01T00:00:00Z',
  };

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const fixture = TestBed.createComponent(EventComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  function flushInitial(http: HttpTestingController, events: EventDto[]) {
    http.expectOne((r) => r.url === eventsUrl && r.params.has('from')).flush(events);
    http.expectOne(remindersUrl).flush([]);
  }

  it('requests events for the visible range and renders them in the calendar', async () => {
    const { fixture, http, el } = setup();
    flushInitial(http, [event]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('app-event-calendar')).not.toBeNull();
    expect(el.textContent).toContain('Passport expires');
    expect(el.textContent).not.toContain('No events this month.');
  });

  it('keeps the calendar and shows an empty state when there are no events', async () => {
    const { fixture, http, el } = setup();
    flushInitial(http, []);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('app-event-calendar')).not.toBeNull();
    expect(el.textContent).toContain('No events this month.');
  });

  it('shows an error with retry when loading fails', async () => {
    const { fixture, http, el } = setup();
    http
      .expectOne((r) => r.url === eventsUrl && r.params.has('from'))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    http.expectOne(remindersUrl).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Could not load events.');
    expect(el.querySelector('app-event-calendar')).not.toBeNull();
  });

  it('shows details with a document link when an event is clicked', async () => {
    const { fixture, http, el } = setup();
    flushInitial(http, [event]);
    await fixture.whenStable();
    fixture.detectChanges();

    (el.querySelector('app-event-calendar li button') as HTMLButtonElement).click();
    fixture.detectChanges();

    const details = el.querySelector('[aria-labelledby="event-details-heading"]');
    expect(details?.textContent).toContain('Passport expires');
    expect(details?.textContent).toContain('Identity');
    expect(details?.textContent).toContain('Renew in time');
    expect(details?.querySelector('a')?.getAttribute('href')).toBe('/documents');
  });

  async function openReminder(setupResult: ReturnType<typeof setup>) {
    const { fixture, http, el } = setupResult;
    http.expectOne((r) => r.url === eventsUrl && r.params.has('from')).flush([event]);
    http.expectOne(remindersUrl).flush([
      {
        id: 5,
        userId: 1,
        eventId: 1,
        documentId: null,
        reminderDate: '2026-12-01T00:00:00Z',
        sentAt: null,
        status: 0,
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();
    (el.querySelector('app-event-calendar li button') as HTMLButtonElement).click();
    fixture.detectChanges();
    const button = Array.from(el.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Delete reminder'),
    ) as HTMLButtonElement;
    return button;
  }

  it('does not delete a reminder when the confirmation is cancelled', async () => {
    const s = setup();
    const button = await openReminder(s);
    const confirmation = TestBed.inject(ConfirmationService);

    button.click();
    expect(confirmation.state()?.title).toBe('Delete reminder?');
    confirmation.cancel();

    s.http.expectNone(`${remindersUrl}5`);
  });

  it('deletes a reminder only after the user confirms', async () => {
    const s = setup();
    const button = await openReminder(s);
    const confirmation = TestBed.inject(ConfirmationService);

    button.click();
    s.http.expectNone(`${remindersUrl}5`);
    confirmation.accept();

    const request = s.http.expectOne(`${remindersUrl}5`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    s.fixture.detectChanges();
    expect(s.el.textContent).toContain('Reminder deleted successfully.');
  });

  it('reloads events for the new range when the month changes', async () => {
    const { fixture, http, el } = setup();
    flushInitial(http, []);
    await fixture.whenStable();
    fixture.detectChanges();

    (el.querySelector('[aria-label="Next month"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    http.expectOne((r) => r.url === eventsUrl && r.params.has('from')).flush([]);
  });

  it('requests a range that covers the whole visible grid', () => {
    const { http } = setup();

    const request = http.expectOne((r) => r.url === eventsUrl && r.params.has('from'));
    const from = new Date(request.request.params.get('from')!);
    const to = new Date(request.request.params.get('to')!);
    const now = new Date();
    const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
    const lastOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59, 999);

    expect(from.getTime()).toBeLessThanOrEqual(firstOfMonth.getTime());
    expect(to.getTime()).toBeGreaterThanOrEqual(lastOfMonth.getTime());
    request.flush([]);
    http.expectOne(remindersUrl).flush([]);
  });

  it('ignores a late response from the previous month', async () => {
    const { fixture, http, el } = setup();
    const first = http.expectOne((r) => r.url === eventsUrl && r.params.has('from'));
    http.expectOne(remindersUrl).flush([]);

    (el.querySelector('[aria-label="Next month"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const second = http.expectOne((r) => r.url === eventsUrl && r.params.has('from'));
    expect(first.cancelled).toBe(true);

    second.flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).not.toContain('Passport expires');
  });

  it('shows the error state again after a month change and recovers on retry', async () => {
    const { fixture, http, el } = setup();
    http
      .expectOne((r) => r.url === eventsUrl && r.params.has('from'))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    http.expectOne(remindersUrl).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const retry = Array.from(el.querySelectorAll('[role="alert"] button')).find(
      (button) => button.textContent?.trim() === 'Try again',
    ) as HTMLButtonElement;
    retry.click();
    fixture.detectChanges();

    http.expectOne((r) => r.url === eventsUrl && r.params.has('from')).flush([event]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')).toBeNull();
    expect(el.textContent).toContain('Passport expires');
  });
});
