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

  it('renders the hero with headline and both calls to action', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('h1')?.textContent).toContain('organized in one place');
    const buttons = Array.from(el.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(buttons.slice(0, 2)).toEqual(['Get Started', 'Log In']);
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

  it('presents the five feature cards', async () => {
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const titles = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li h3')).map(
      (h) => h.textContent?.trim(),
    );

    expect(titles).toEqual([
      'Personal Documents',
      'Receipts and Purchases',
      'Personal Information',
      'Reminders and Important Dates',
      'Everyday Administration',
    ]);
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
      '/documents',
      '/documents',
      '/dashboard',
      '/events',
      '/dashboard',
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
