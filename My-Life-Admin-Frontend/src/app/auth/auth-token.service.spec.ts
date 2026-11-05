import { TestBed } from '@angular/core/testing';
import { AuthTokenService } from './auth-token.service';

function makeToken(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({ exp }));
  return `${header}.${payload}.signature`;
}

describe('AuthTokenService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  afterEach(() => localStorage.clear());

  it('stores the token and sets isLoggedIn to true', () => {
    const service = TestBed.inject(AuthTokenService);

    service.setToken('jwt-token');

    expect(localStorage.getItem('my-life-admin.auth-token')).toBe('jwt-token');
    expect(service.isLoggedIn()).toBe(true);
  });

  it('throws when setting an empty token', () => {
    const service = TestBed.inject(AuthTokenService);

    expect(() => service.setToken('')).toThrow();
    expect(service.isLoggedIn()).toBe(false);
  });

  it('removes the token and sets isLoggedIn to false', () => {
    const service = TestBed.inject(AuthTokenService);
    service.setToken('jwt-token');

    service.clearToken();

    expect(localStorage.getItem('my-life-admin.auth-token')).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });

  it('returns the stored token', () => {
    const service = TestBed.inject(AuthTokenService);
    service.setToken('jwt-token');

    expect(service.getToken()).toBe('jwt-token');
  });

  it('returns null when no token is stored', () => {
    const service = TestBed.inject(AuthTokenService);

    expect(service.getToken()).toBeNull();
  });

  it('clears and returns null for an expired token', () => {
    const service = TestBed.inject(AuthTokenService);
    service.setToken(makeToken(Math.floor(Date.now() / 1000) - 3600));

    expect(service.getToken()).toBeNull();
    expect(localStorage.getItem('my-life-admin.auth-token')).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });

  it('keeps a valid, non-expired token', () => {
    const service = TestBed.inject(AuthTokenService);
    const token = makeToken(Math.floor(Date.now() / 1000) + 3600);
    service.setToken(token);

    expect(service.getToken()).toBe(token);
    expect(service.isLoggedIn()).toBe(true);
  });
});
