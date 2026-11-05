import { Component, inject, signal } from '@angular/core';
import { SettingsDto } from '../models/settings.dto';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { ThemeService } from '../shared/theme/theme.service';
import { SettingsService } from './settings.service';

@Component({
  selector: 'app-settings-page',
  imports: [ErrorState, LoadingState],
  templateUrl: './settings-page.html',
})
export class SettingsPage {
  private readonly settingsService = inject(SettingsService);
  private readonly themeService = inject(ThemeService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly settings = signal<SettingsDto | null>(null);

  protected readonly darkMode = signal(false);
  protected readonly emailNotifications = signal(true);

  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly saved = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.settingsService.getSettings().subscribe({
      next: (settings) => {
        this.apply(settings);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  protected setDarkMode(value: boolean): void {
    this.darkMode.set(value);
    this.themeService.setDarkMode(value);
    this.saved.set(false);
  }

  protected setEmailNotifications(value: boolean): void {
    this.emailNotifications.set(value);
    this.saved.set(false);
  }

  protected save(): void {
    const current = this.settings();

    if (!current) {
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);

    this.settingsService
      .updateSettings({
        ...current,
        darkMode: this.darkMode(),
        emailNotifications: this.emailNotifications(),
      })
      .subscribe({
        next: (settings) => {
          this.apply(settings);
          this.saving.set(false);
          this.saved.set(true);
        },
        error: () => {
          this.saving.set(false);
          this.saved.set(false);
          this.saveError.set('Could not save your settings. Please try again.');
        },
      });
  }

  private apply(settings: SettingsDto): void {
    this.settings.set(settings);
    this.darkMode.set(settings.darkMode);
    this.themeService.setDarkMode(settings.darkMode);
    this.emailNotifications.set(settings.emailNotifications);
  }
}
