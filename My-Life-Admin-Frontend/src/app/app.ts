import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoginModal } from './login/login-modal';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LoginModal],
  templateUrl: './app.html',
})
export class App {
  private http = inject(HttpClient);
  protected readonly status = signal('Checking…');
  protected readonly showLogin = signal(false);

  constructor() {
    this.http.get<{ status: string }>('http://localhost:5076/api/health').subscribe({
      next: (res) => this.status.set(res.status),
      error: () => this.status.set('Offline'),
    });
  }
}
