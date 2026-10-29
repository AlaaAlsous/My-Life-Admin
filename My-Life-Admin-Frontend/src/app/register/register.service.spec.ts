import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RegisterService } from './register.service';

describe('RegisterService', () => {
  const registerUrl = 'http://localhost:5076/api/auth/register';
  const loginUrl = 'http://localhost:5076/api/auth/login';
  const request = {
    name: 'Anna Andersson',
    email: 'anna@example.com',
    password: 'secret123',
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('registers the user and then calls the login service', () => {
    const service = TestBed.inject(RegisterService);
    const http = TestBed.inject(HttpTestingController);

    service.register(request).subscribe();

    const registerReq = http.expectOne(registerUrl);
    expect(registerReq.request.method).toBe('POST');
    expect(registerReq.request.body).toEqual(request);
    http.expectNone(loginUrl);

    registerReq.flush({});

    const loginReq = http.expectOne(loginUrl);
    expect(loginReq.request.method).toBe('POST');
    expect(loginReq.request.body).toEqual({
      email: request.email,
      password: request.password,
    });
    loginReq.flush({
      id: 7,
      name: request.name,
      email: request.email,
      token: 'jwt-token-value',
    });

    http.verify();
  });
});
