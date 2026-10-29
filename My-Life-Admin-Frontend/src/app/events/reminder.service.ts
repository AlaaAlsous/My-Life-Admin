import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateReminderDto,
  ReminderDto,
  UpdateReminderDto,
} from '../models/reminder.dto';

@Injectable({ providedIn: 'root' })
export class ReminderService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/reminders/`;

  getAll(): Observable<ReminderDto[]> {
    return this.http.get<ReminderDto[]>(this.url);
  }

  getPending(): Observable<ReminderDto[]> {
    return this.http.get<ReminderDto[]>(`${this.url}pending`);
  }

  getById(id: number): Observable<ReminderDto> {
    return this.http.get<ReminderDto>(`${this.url}${id}`);
  }

  create(reminder: CreateReminderDto): Observable<ReminderDto> {
    return this.http.post<ReminderDto>(this.url, reminder);
  }

  update(id: number, reminder: UpdateReminderDto): Observable<ReminderDto> {
    return this.http.put<ReminderDto>(`${this.url}${id}`, reminder);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}${id}`);
  }
}
