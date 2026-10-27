import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AppLayout } from './layout/app-layout';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [AppLayout],
  templateUrl: './app.html',
})
export class App {
  private http = inject(HttpClient);
  protected readonly status = signal('Checking…');

  constructor() {
    this.http.get<{ status: string }>(`${environment.apiUrl}/api/health`).subscribe({
      next: (res) => this.status.set(res.status),
      error: () => this.status.set('Offline'),
    });
  }
}
