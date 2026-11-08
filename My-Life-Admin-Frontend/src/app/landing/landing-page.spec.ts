import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthTokenService } from '../auth/auth-token.service';
import { LandingPage } from './landing-page';

describe('LandingPage', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LandingPage],
      providers: [provideRouter([])],
    }).compileComponents();
  });
  afterEach(() => localStorage.clear());

  it('renders a compact hero with the headline and no call-to-action buttons', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const hero = fixture.nativeElement.querySelector(
      'section[aria-labelledby="hero-heading"]',
    ) as HTMLElement;

    expect(hero.querySelector('h1')?.textContent).toContain('organized in one place');
    expect(hero.querySelectorAll('button').length).toBe(0);
  });

  it('hides sign-up calls to action and the promotional section when logged in', async () => {
    TestBed.inject(AuthTokenService).setToken('jwt-token');
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('section[aria-labelledby="cta-heading"]')).toBeNull();
    expect(
      Array.from(el.querySelectorAll('button')).map((button) => button.textContent?.trim()),
    ).not.toContain('Get Started');
    expect(
      Array.from(el.querySelectorAll('button')).map((button) => button.textContent?.trim()),
    ).not.toContain('Log In');
  });

  it('presents the four feature cards', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const titles = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li h3')).map(
      (h) => h.textContent?.trim(),
    );

    expect(titles).toEqual(['Dashboard', 'Documents', 'Events', 'Settings']);
  });

  it('links feature cards to their pages only when logged in', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelectorAll('li a[href]').length).toBe(0);

    TestBed.inject(AuthTokenService).setToken('jwt-token');
    fixture.detectChanges();

    const links = Array.from(el.querySelectorAll('li a'));
    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/dashboard',
      '/documents',
      '/events',
      '/settings',
    ]);
    expect(links.every((link) => link.querySelector('svg'))).toBe(true);
  });

  it('opens the login modal from a call to action', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    el.querySelector('button')!.click();
    await fixture.whenStable();

    expect(el.querySelector('app-login-modal')).not.toBeNull();
  });
});
