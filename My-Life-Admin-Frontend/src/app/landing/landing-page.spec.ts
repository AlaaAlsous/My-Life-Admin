import { TestBed } from '@angular/core/testing';
import { AuthTokenService } from '../auth/auth-token.service';
import { LandingPage } from './landing-page';

describe('LandingPage', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('renders the hero with headline and both calls to action', async () => {
    await TestBed.configureTestingModule({ imports: [LandingPage] }).compileComponents();
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('h1')?.textContent).toContain('organized in one place');
    const buttons = Array.from(el.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(buttons.slice(0, 2)).toEqual(['Get Started', 'Log In']);
  });

  it('hides sign-up calls to action and the promotional section when logged in', async () => {
    await TestBed.configureTestingModule({ imports: [LandingPage] }).compileComponents();
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
    await TestBed.configureTestingModule({ imports: [LandingPage] }).compileComponents();
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

  it('opens the login modal from a call to action', async () => {
    await TestBed.configureTestingModule({ imports: [LandingPage] }).compileComponents();
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    el.querySelector('button')!.click();
    await fixture.whenStable();

    expect(el.querySelector('app-login-modal')).not.toBeNull();
  });
});
