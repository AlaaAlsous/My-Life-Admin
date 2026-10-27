import { TestBed } from '@angular/core/testing';
import { LandingPage } from './landing-page';

describe('LandingPage', () => {
  it('renders the hero with headline and both calls to action', async () => {
    await TestBed.configureTestingModule({ imports: [LandingPage] }).compileComponents();
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('h1')?.textContent).toContain('organized in one place');
    const buttons = Array.from(el.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(buttons).toEqual(['Get Started', 'Log In']);
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
