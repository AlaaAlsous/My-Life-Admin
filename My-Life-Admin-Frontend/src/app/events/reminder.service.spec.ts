import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReminderDto, ReminderStatus } from '../models/reminder.dto';
import { ReminderService } from './reminder.service';

describe('ReminderService', () => {
  const remindersUrl = 'http://localhost:5076/api/reminders/';
  let service: ReminderService;
  let http: HttpTestingController;

  const reminder: ReminderDto = {
    id: 4,
    userId: 7,
    eventId: 2,
    documentId: null,
    reminderDate: '2026-11-10T09:00:00Z',
    sentAt: null,
    status: ReminderStatus.Pending,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ReminderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets all reminders', () => {
    service.getAll().subscribe((result) => {
      expect(result).toEqual([reminder]);
    });

    const request = http.expectOne(remindersUrl);
    expect(request.request.method).toBe('GET');
    request.flush([reminder]);
  });

  it('gets pending reminders', () => {
    service.getPending().subscribe((result) => {
      expect(result).toEqual([reminder]);
    });

    const request = http.expectOne(`${remindersUrl}pending`);
    expect(request.request.method).toBe('GET');
    request.flush([reminder]);
  });

  it('gets a reminder by id', () => {
    service.getById(4).subscribe((result) => {
      expect(result).toEqual(reminder);
    });

    const request = http.expectOne(`${remindersUrl}4`);
    expect(request.request.method).toBe('GET');
    request.flush(reminder);
  });

  it('creates a reminder', () => {
    const body = { eventId: 2, documentId: null, reminderDate: '2026-11-10T09:00:00Z' };

    service.create(body).subscribe((result) => {
      expect(result).toEqual(reminder);
    });

    const request = http.expectOne(remindersUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(reminder);
  });

  it('updates a reminder', () => {
    const body = { eventId: 2, documentId: null, reminderDate: '2026-11-12T09:00:00Z' };

    service.update(4, body).subscribe((result) => {
      expect(result.reminderDate).toBe(body.reminderDate);
    });

    const request = http.expectOne(`${remindersUrl}4`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ ...reminder, reminderDate: body.reminderDate });
  });

  it('deletes a reminder', () => {
    service.delete(4).subscribe();

    const request = http.expectOne(`${remindersUrl}4`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
