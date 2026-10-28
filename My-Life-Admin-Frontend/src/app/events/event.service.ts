import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateEventDto, EventDto, UpdateEventDto } from '../models/event.dto';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/events/`;

  getAll(): Observable<EventDto[]> {
    return this.http.get<EventDto[]>(this.url);
  }

  getBetween(from: string, to: string): Observable<EventDto[]> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<EventDto[]>(this.url, { params });
  }

  getById(id: number): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.url}${id}`);
  }

  create(event: CreateEventDto): Observable<EventDto> {
    return this.http.post<EventDto>(this.url, event);
  }

  update(id: number, event: UpdateEventDto): Observable<EventDto> {
    return this.http.put<EventDto>(`${this.url}${id}`, event);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}${id}`);
  }
}
