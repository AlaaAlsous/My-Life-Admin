import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
})
export class App {
  private http = inject(HttpClient);
  protected readonly status = signal('Checking…');

  constructor() {
    this.http.get<{ status: string }>('http://localhost:5076/api/health').subscribe({
      next: (res) => this.status.set(res.status),
      error: () => this.status.set('Offline'),
    });
  }
}
