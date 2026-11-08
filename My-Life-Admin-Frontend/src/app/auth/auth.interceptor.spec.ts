import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthTokenService } from './auth-token.service';

@Component({ template: '' })
class TestComponent {}

function makeToken(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({ exp }));
  return `${header}.${payload}.signature`;
}

describe('authInterceptor', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: '', component: TestComponent }]),
      ],
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('adds bearer token to backend API requests', () => {
    const tokenService = TestBed.inject(AuthTokenService);
    tokenService.setToken('jwt-token');

    const http = TestBed.inject(HttpClient);
    const testing = TestBed.inject(HttpTestingController);

    http.get('http://localhost:5076/api/health').subscribe();

    const req = testing.expectOne('http://localhost:5076/api/health');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    req.flush({ status: 'Healthy' });
    testing.verify();
  });

  it('does not add bearer token to login/register endpoints', () => {
    const tokenService = TestBed.inject(AuthTokenService);
    tokenService.setToken('jwt-token');

    const http = TestBed.inject(HttpClient);
    const testing = TestBed.inject(HttpTestingController);

    http.post('http://localhost:5076/api/auth/login', {}).subscribe();
    http.post('http://localhost:5076/api/auth/register', {}).subscribe();

    const loginReq = testing.expectOne('http://localhost:5076/api/auth/login');
    const registerReq = testing.expectOne('http://localhost:5076/api/auth/register');
    expect(loginReq.request.headers.has('Authorization')).toBe(false);
    expect(registerReq.request.headers.has('Authorization')).toBe(false);

    loginReq.flush({});
    registerReq.flush({});
    testing.verify();
  });

  it('clears the token when a backend request returns 401', () => {
    const tokenService = TestBed.inject(AuthTokenService);
    tokenService.setToken(makeToken(Math.floor(Date.now() / 1000) + 3600));

    const http = TestBed.inject(HttpClient);
    const testing = TestBed.inject(HttpTestingController);

    let errored = false;
    http.get('http://localhost:5076/api/documents/').subscribe({
      error: () => (errored = true),
    });

    const req = testing.expectOne('http://localhost:5076/api/documents/');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBe(true);
    expect(tokenService.getToken()).toBeNull();
    testing.verify();
  });
});
