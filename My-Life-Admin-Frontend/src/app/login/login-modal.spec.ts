import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoginModal } from './login-modal';

const loginUrl = 'http://localhost:5076/api/auth/login';

async function setup() {
  await TestBed.configureTestingModule({
    imports: [LoginModal],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  }).compileComponents();

  const fixture = TestBed.createComponent(LoginModal);
  fixture.detectChanges();
  const el = fixture.nativeElement as HTMLElement;
  const http = TestBed.inject(HttpTestingController);

  const fill = (email: string, password: string) => {
    const email$ = el.querySelector<HTMLInputElement>('#login-email')!;
    const password$ = el.querySelector<HTMLInputElement>('#login-password')!;
    email$.value = email;
    email$.dispatchEvent(new Event('input'));
    password$.value = password;
    password$.dispatchEvent(new Event('input'));
  };
  const submit = () => {
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  return { fixture, el, http, fill, submit };
}

describe('LoginModal', () => {
  it('is an accessible dialog with English labels', async () => {
    const { el } = await setup();
    const dialog = el.querySelector('[role="dialog"]')!;

    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(el.querySelector('#login-title')?.textContent).toContain('Log in');
    expect(el.querySelector('label[for="login-email"]')?.textContent).toContain('Email');
    expect(el.querySelector('label[for="login-password"]')?.textContent).toContain('Password');
    expect(el.querySelector('button[aria-label="Close"]')).toBeTruthy();
  });

  it('shows English validation messages and does not call the API', async () => {
    const { fixture, el, http, submit } = await setup();

    submit();
    expect(el.textContent).toContain('Email is required.');
    expect(el.textContent).toContain('Password is required.');

    http.expectNone(loginUrl);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows invalid email and short password messages', async () => {
    const { fixture, el, fill, submit } = await setup();

    fill('not-an-email', '123');
    submit();
    fixture.detectChanges();

    expect(el.textContent).toContain('Enter a valid email address.');
    expect(el.textContent).toContain('Password must be at least 6 characters.');
  });

  it('emits loggedIn and closed on successful login', async () => {
    const { fixture, http, fill, submit } = await setup();
    const loggedIn = vi.fn();
    const closed = vi.fn();
    fixture.componentInstance.loggedIn.subscribe(loggedIn);
    fixture.componentInstance.closed.subscribe(closed);

    fill('user@example.com', 'secret123');
    submit();
    const req = http.expectOne(loginUrl);
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'secret123' });
    req.flush({ token: 'abc' });

    expect(loggedIn).toHaveBeenCalled();
    expect(closed).toHaveBeenCalled();
  });

  it('shows English errors for failed login', async () => {
    const { fixture, el, http, fill, submit } = await setup();
    fill('user@example.com', 'secret123');

    submit();
    http.expectOne(loginUrl).flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(el.textContent).toContain('Incorrect email or password.');

    submit();
    http.expectOne(loginUrl).error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(el.textContent).toContain('Could not reach the server. Please try again later.');

    submit();
    http.expectOne(loginUrl).flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(el.textContent).toContain('Something went wrong while logging in. Please try again.');
  });

  it('emits closed when the close button is clicked', async () => {
    const { fixture, el } = await setup();
    const closed = vi.fn();
    fixture.componentInstance.closed.subscribe(closed);

    el.querySelector<HTMLButtonElement>('button[aria-label="Close"]')!.click();

    expect(closed).toHaveBeenCalled();
  });
});
