import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EventService } from './event.service';

describe('EventService', () => {
  const eventsUrl = 'http://localhost:5076/api/events/';
  let service: EventService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(EventService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets all events, filters by date range, and gets an event by ID', () => {
    service.getAll().subscribe();
    http.expectOne(eventsUrl).flush([]);

    service.getBetween('2026-10-01T00:00:00Z', '2026-10-31T23:59:59Z').subscribe();
    const rangeRequest = http.expectOne(
      `${eventsUrl}?from=2026-10-01T00:00:00Z&to=2026-10-31T23:59:59Z`,
    );
    rangeRequest.flush([]);

    service.getById(12).subscribe();
    http.expectOne(`${eventsUrl}12`).flush({});
  });

  it('creates and updates events with the event DTO payload', () => {
    const event = {
      documentId: 7,
      title: 'Renew passport',
      description: 'Book an appointment',
      category: 'Personal',
      eventDate: '2026-11-15T10:00:00Z',
    };

    service.create(event).subscribe();
    const createRequest = http.expectOne(eventsUrl);
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual(event);
    createRequest.flush({});

    service.update(12, event).subscribe();
    const updateRequest = http.expectOne(`${eventsUrl}12`);
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(event);
    updateRequest.flush({});
  });

  it('deletes an event by ID', () => {
    service.delete(12).subscribe();
    const deleteRequest = http.expectOne(`${eventsUrl}12`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);
  });
});
