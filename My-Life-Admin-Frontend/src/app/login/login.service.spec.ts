import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthTokenService } from '../auth/auth-token.service';
import { LoginService } from './login.service';

describe('LoginService', () => {
  const loginUrl = 'http://localhost:5076/api/auth/login';

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('stores the JWT token from a successful login', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    const service = TestBed.inject(LoginService);
    const http = TestBed.inject(HttpTestingController);
    const tokenService = TestBed.inject(AuthTokenService);

    service.login({ email: 'anna@example.com', password: 'secret123' }).subscribe();

    const req = http.expectOne(loginUrl);
    req.flush({
      id: 7,
      name: 'Anna',
      email: 'anna@example.com',
      token: 'jwt-token-value',
    });

    expect(tokenService.getToken()).toBe('jwt-token-value');
    expect(tokenService.getUserName()).toBe('Anna');
    http.verify();
  });
});
