import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EventDto } from '../models/event.dto';
import { EventComponent } from './event.component';

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

  it('reloads events for the new range when the month changes', async () => {
    const { fixture, http, el } = setup();
    flushInitial(http, []);
    await fixture.whenStable();
    fixture.detectChanges();

    (el.querySelector('[aria-label="Next month"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    http.expectOne((r) => r.url === eventsUrl && r.params.has('from')).flush([]);
  });
});
