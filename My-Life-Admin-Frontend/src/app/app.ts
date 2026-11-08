import { HttpClient } from '@angular/common/http';
import { Component, effect, inject, signal } from '@angular/core';
import { AuthTokenService } from './auth/auth-token.service';
import { AppLayout } from './layout/app-layout';
import { ConfirmationModal } from './shared/confirmation/confirmation-modal';
import { SettingsService } from './settings/settings.service';
import { ThemeService } from './shared/theme/theme.service';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [AppLayout, ConfirmationModal],
  templateUrl: './app.html',
})
export class App {
  private http = inject(HttpClient);
  private readonly authTokenService = inject(AuthTokenService);
  private readonly settingsService = inject(SettingsService);
  private readonly theme = inject(ThemeService);
  protected readonly status = signal('Checking…');
  protected readonly themeError = signal<string | null>(null);

  constructor() {
    this.http.get<{ status: string }>(`${environment.apiUrl}/api/health`).subscribe({
      next: (res) => this.status.set(res.status),
      error: () => this.status.set('Offline'),
    });

    effect((onCleanup) => {
      if (!this.authTokenService.isLoggedIn()) {
        this.themeError.set(null);
        return;
      }

      this.themeError.set(null);
      const subscription = this.settingsService.getSettings().subscribe({
        next: (settings) => this.theme.setDarkMode(settings.darkMode),
        error: () =>
          this.themeError.set(
            'Could not load appearance preferences. Your last selected theme is still in use.',
          ),
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }
}
