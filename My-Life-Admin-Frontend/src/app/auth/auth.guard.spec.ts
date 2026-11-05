import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { AuthTokenService } from './auth-token.service';
import { authGuard } from './auth.guard';

function makeToken(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({ exp }));
  return `${header}.${payload}.signature`;
}

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  afterEach(() => localStorage.clear());

  const run = () => TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

  it('redirects to the landing page without a token', () => {
    const result = run();
    expect(result instanceof UrlTree).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/');
  });

  it('allows navigation with a token', () => {
    TestBed.inject(AuthTokenService).setToken('jwt');
    expect(run()).toBe(true);
  });

  it('redirects to the landing page when the token has expired', () => {
    TestBed.inject(AuthTokenService).setToken(makeToken(Math.floor(Date.now() / 1000) - 3600));

    const result = run();

    expect(result instanceof UrlTree).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/');
  });
});
