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

  function saveButton(el: HTMLElement) {
    return [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) =>
      b.textContent?.includes('Save settings'),
    )!;
  }

  it('loads and shows the saved settings', async () => {
    const { el } = await loaded();

    expect(el.querySelector<HTMLInputElement>('#setting-email')!.checked).toBe(true);
    expect(el.querySelector<HTMLInputElement>('#setting-dark-mode')!.checked).toBe(false);
  });

  it('applies saved dark mode and automatically persists changes', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(settingsUrl).flush({ ...settings, darkMode: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(localStorage.getItem('my-life-admin.dark-mode')).toBe('true');

    toggle(el, '#setting-dark-mode');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    expect(localStorage.getItem('my-life-admin.dark-mode')).toBe('false');

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ ...settings, darkMode: false });
    request.flush({ ...settings, darkMode: false });
    await fixture.whenStable();

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

  it('saves the changed settings', async () => {
    const { fixture, http, el } = await loaded();

    toggle(el, '#setting-dark-mode');
    const darkModeRequest = http.expectOne(settingsUrl);
    expect(darkModeRequest.request.body).toEqual({ ...settings, darkMode: true });
    darkModeRequest.flush({ ...settings, darkMode: true });
    await fixture.whenStable();

    toggle(el, '#setting-email');
    fixture.detectChanges();
    saveButton(el).click();

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      ...settings,
      darkMode: true,
      emailNotifications: false,
    });
    request.flush({ ...settings, darkMode: true, emailNotifications: false });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Your settings have been saved.');
    expect(el.querySelector<HTMLInputElement>('#setting-dark-mode')!.checked).toBe(true);
  });

  it('shows an error when saving fails', async () => {
    const { fixture, http, el } = await loaded();

    saveButton(el).click();
    http.expectOne(settingsUrl).flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Could not save your settings. Please try again.');
    expect(el.textContent).not.toContain('Your settings have been saved.');
  });
});
