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

  it('gets all events without query parameters', () => {
    service.getAll().subscribe();

    const request = http.expectOne(eventsUrl);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual([]);
    request.flush([]);
  });

  it('sends from and to when a range is given', () => {
    const from = '2026-09-28T00:00:00.000Z';
    const to = '2026-11-01T22:59:59.999Z';

    service.getAll(from, to).subscribe();

    const request = http.expectOne((r) => r.url === eventsUrl);
    expect(request.request.params.get('from')).toBe(from);
    expect(request.request.params.get('to')).toBe(to);
    request.flush([]);
  });

  it('gets, creates, updates and deletes a single event', () => {
    const body = {
      documentId: null,
      title: 'Passport expires',
      description: null,
      category: 'Identity',
      eventDate: '2026-12-01T12:00:00Z',
    };

    service.getById(3).subscribe();
    expect(http.expectOne(`${eventsUrl}3`).request.method).toBe('GET');

    service.create(body).subscribe();
    const create = http.expectOne(eventsUrl);
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual(body);

    service.update(3, body).subscribe();
    const update = http.expectOne(`${eventsUrl}3`);
    expect(update.request.method).toBe('PUT');

    service.delete(3).subscribe();
    const remove = http.expectOne(`${eventsUrl}3`);
    expect(remove.request.method).toBe('DELETE');

    http.match(() => true).forEach((r) => r.flush({}));
  });
});
