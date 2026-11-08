import { Injectable, signal } from '@angular/core';

const storageKey = 'my-life-admin.dark-mode';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly darkModeSignal = signal(localStorage.getItem(storageKey) === 'true');

  constructor() {
    this.apply(this.darkModeSignal());
  }

  setDarkMode(enabled: boolean): void {
    this.darkModeSignal.set(enabled);
    localStorage.setItem(storageKey, String(enabled));
    this.apply(enabled);
  }

  private apply(enabled: boolean): void {
    document.documentElement.classList.toggle('dark', enabled);
    document
      .querySelector<HTMLMetaElement>('meta[name="theme-color"]')
      ?.setAttribute('content', enabled ? '#171216' : '#8c4a5e');
  }
}
