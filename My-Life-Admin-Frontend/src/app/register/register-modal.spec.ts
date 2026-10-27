import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthTokenService } from '../auth/auth-token.service';
import { RegisterModal } from './register-modal';

const registerUrl = 'http://localhost:5076/api/auth/register';
const loginUrl = 'http://localhost:5076/api/auth/login';

async function setup() {
  localStorage.clear();
  await TestBed.configureTestingModule({
    imports: [RegisterModal],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  }).compileComponents();

  const fixture = TestBed.createComponent(RegisterModal);
  fixture.detectChanges();
  const el = fixture.nativeElement as HTMLElement;
  const http = TestBed.inject(HttpTestingController);

  const setValue = (selector: string, value: string) => {
    const input = el.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  };
  const fill = (name: string, email: string, password: string) => {
    setValue('#register-name', name);
    setValue('#register-email', email);
    setValue('#register-password', password);
  };
  const submit = () => {
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  return { fixture, el, http, fill, submit };
}

describe('RegisterModal', () => {
  afterEach(() => localStorage.clear());

  it('shows validation messages and does not call the API', async () => {
    const { el, http, submit } = await setup();

    submit();

    expect(el.textContent).toContain('Name is required.');
    expect(el.textContent).toContain('Email is required.');
    expect(el.textContent).toContain('Password is required.');
    http.expectNone(registerUrl);
  });

  it('registers, logs in and emits loggedIn', async () => {
    const { fixture, http, fill, submit } = await setup();
    let loggedIn = 0;
    fixture.componentInstance.loggedIn.subscribe(() => loggedIn++);

    fill('Anna', 'anna@example.com', 'secret123');
    submit();

    const registerReq = http.expectOne(registerUrl);
    expect(registerReq.request.body).toEqual({
      name: 'Anna',
      email: 'anna@example.com',
      password: 'secret123',
    });
    registerReq.flush(
      { id: 1, name: 'Anna', email: 'anna@example.com' },
      { status: 201, statusText: 'Created' },
    );

    http
      .expectOne(loginUrl)
      .flush({ id: 1, name: 'Anna', email: 'anna@example.com', token: 'jwt' });

    expect(TestBed.inject(AuthTokenService).getToken()).toBe('jwt');
    expect(loggedIn).toBe(1);
  });

  it('shows a message when the email is already registered', async () => {
    const { fixture, el, http, fill, submit } = await setup();

    fill('Anna', 'anna@example.com', 'secret123');
    submit();
    http
      .expectOne(registerUrl)
      .flush({ error: 'Email is already registered.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(el.textContent).toContain('This email is already registered.');
  });
});
