import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Routes, provideRouter } from '@angular/router';
import { AuthTokenService } from '../auth/auth-token.service';
import { AppLayout } from './app-layout';

@Component({ template: '<p>Tasks page</p>' })
class TasksPage {}

async function setup(routes: Routes = []) {
  await TestBed.configureTestingModule({
    imports: [AppLayout],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
  }).compileComponents();

  const fixture = TestBed.createComponent(AppLayout);
  fixture.detectChanges();
  return { fixture, el: fixture.nativeElement as HTMLElement };
}

describe('AppLayout', () => {
  it('renders header, main with router outlet, and footer', async () => {
    const { el } = await setup();

    expect(el.querySelector('header')).toBeTruthy();
    expect(el.querySelector('main router-outlet')).toBeTruthy();
    expect(el.querySelector('footer')).toBeTruthy();
  });

  it('shows no navigation when no routes exist', async () => {
    const { el } = await setup();

    expect(el.querySelector('header nav')).toBeNull();
    expect(el.querySelector('button[aria-controls="main-nav"]')).toBeNull();
  });

  it('shows navigation links only for existing routes', async () => {
    const { el } = await setup([
      { path: 'tasks', component: TasksPage },
      { path: '', redirectTo: 'tasks', pathMatch: 'full' },
      { path: '**', redirectTo: '' },
    ]);

    const links = Array.from(el.querySelectorAll('header nav a'));
    expect(links.length).toBe(1);
    expect(links[0].textContent).toContain('Tasks');
    expect(links[0].getAttribute('href')).toBe('/tasks');
    expect(el.querySelector('header nav')?.getAttribute('aria-label')).toBe('Main navigation');
  });

  it('toggles the mobile menu with aria-expanded', async () => {
    const { fixture, el } = await setup([{ path: 'tasks', component: TasksPage }]);
    const toggle = el.querySelector<HTMLButtonElement>('button[aria-controls="main-nav"]')!;

    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(el.querySelector('header nav')?.classList).toContain('hidden');

    toggle.click();
    fixture.detectChanges();

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(el.querySelector('header nav')?.classList).not.toContain('hidden');
  });

  it('shows app info and navigation links in the footer', async () => {
    const { el } = await setup();
    const footer = el.querySelector('footer')!;

    expect(footer.textContent).toContain('My-Life-Admin');
    expect(footer.querySelector('nav')?.getAttribute('aria-label')).toBe('Footer navigation');
    expect(footer.querySelector('a')?.textContent).toContain('Features');
  });

  it('shows the backend status in the footer', async () => {
    const { fixture, el } = await setup();
    fixture.componentRef.setInput('status', 'Healthy');
    fixture.detectChanges();

    expect(el.querySelector('footer')?.textContent).toContain('Backend status: Healthy');
  });

  it('shows Log out and guarded links when logged in, and logs out', async () => {
    localStorage.clear();
    localStorage.setItem('my-life-admin.auth-token', 'jwt');
    const { fixture, el } = await setup([
      { path: 'tasks', component: TasksPage, canActivate: [() => true] },
    ]);

    expect(el.querySelector('header nav a')?.textContent).toContain('Tasks');
    const logout = Array.from(el.querySelectorAll<HTMLButtonElement>('header button')).find(
      (b) => b.textContent?.trim() === 'Log out',
    )!;
    expect(logout).toBeTruthy();

    logout.click();
    fixture.detectChanges();

    expect(TestBed.inject(AuthTokenService).getToken()).toBeNull();
    expect(el.querySelector('header nav')).toBeNull();
    localStorage.clear();
  });

  it('hides guarded links when logged out', async () => {
    localStorage.clear();
    const { el } = await setup([
      { path: 'tasks', component: TasksPage, canActivate: [() => true] },
    ]);

    expect(el.querySelector('header nav')).toBeNull();
  });

  it('opens and closes the login modal from the header button', async () => {
    const { fixture, el } = await setup();
    const loginButton = Array.from(el.querySelectorAll<HTMLButtonElement>('header button')).find(
      (b) => b.textContent?.trim() === 'Log in',
    )!;

    expect(el.querySelector('app-login-modal')).toBeNull();

    loginButton.click();
    fixture.detectChanges();
    expect(el.querySelector('[role="dialog"]')).toBeTruthy();

    el.querySelector<HTMLButtonElement>('button[aria-label="Close"]')!.click();
    fixture.detectChanges();
    expect(el.querySelector('app-login-modal')).toBeNull();
  });
});
