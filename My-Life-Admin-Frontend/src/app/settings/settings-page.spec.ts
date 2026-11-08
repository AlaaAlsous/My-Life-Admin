import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SettingsPage } from './settings-page';

describe('SettingsPage', () => {
  const settingsUrl = 'http://localhost:5076/api/settings';
  const settings = { id: 3, userId: 7, darkMode: false, emailNotifications: true };

  beforeEach(() => {
    localStorage.removeItem('my-life-admin.dark-mode');
    document.documentElement.classList.remove('dark');
  });

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(SettingsPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  async function loaded() {
    const context = setup();
    context.http.expectOne(settingsUrl).flush(settings);
    await context.fixture.whenStable();
    context.fixture.detectChanges();
    return context;
  }

  function toggle(el: HTMLElement, id: string) {
    const input = el.querySelector<HTMLInputElement>(id)!;
    input.checked = !input.checked;
    input.dispatchEvent(new Event('change'));
  }

  it('loads and shows the saved settings', async () => {
    const { el } = await loaded();

    expect(el.querySelector<HTMLInputElement>('#setting-email')!.checked).toBe(true);
    expect(el.querySelector<HTMLInputElement>('#setting-dark-mode')!.checked).toBe(false);
  });

  it('applies and saves dark mode immediately when toggled', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(settingsUrl).flush({ ...settings, darkMode: true });
    await fixture.whenStable();
    fixture.detectChanges();

    // The saved theme is applied on load.
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(localStorage.getItem('my-life-admin.dark-mode')).toBe('true');

    // Toggling applies the theme and saves immediately.
    toggle(el, '#setting-dark-mode');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    expect(localStorage.getItem('my-life-admin.dark-mode')).toBe('false');

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ ...settings, darkMode: false });
    request.flush({ ...settings, darkMode: false });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Your settings have been saved.');
  });

  it('shows an error with retry when loading fails', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(settingsUrl).flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('We could not load your settings.');

    el.querySelector<HTMLButtonElement>('button')!.click();
    http.expectOne(settingsUrl).flush(settings);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('#setting-email')).not.toBeNull();
  });

  it('saves email notification changes immediately when toggled', async () => {
    const { fixture, http, el } = await loaded();

    toggle(el, '#setting-email');

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ ...settings, emailNotifications: false });
    request.flush({ ...settings, emailNotifications: false });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Your settings have been saved.');
    expect(el.querySelector<HTMLInputElement>('#setting-email')!.checked).toBe(false);
  });

  it('shows an error when saving fails', async () => {
    const { fixture, http, el } = await loaded();

    toggle(el, '#setting-email');
    http.expectOne(settingsUrl).flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Could not save your settings. Please try again.');
    expect(el.textContent).not.toContain('Your settings have been saved.');
  });
});
