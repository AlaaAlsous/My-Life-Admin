import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SettingsDto } from '../models/settings.dto';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/settings`;

  getSettings(): Observable<SettingsDto> {
    return this.http.get<SettingsDto>(this.url);
  }

  updateSettings(settings: SettingsDto): Observable<void> {
    return this.http.put<void>(this.url, settings);
  }
}
