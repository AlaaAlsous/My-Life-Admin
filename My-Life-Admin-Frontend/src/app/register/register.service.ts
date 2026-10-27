import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { LoginResponse, LoginService } from '../login/login.service';

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class RegisterService {
  private readonly http = inject(HttpClient);
  private readonly loginService = inject(LoginService);
  private readonly url = 'http://localhost:5076/api/auth/register';

  // Registers the user and then logs in, so they land on the dashboard right away.
  register(request: RegisterRequest): Observable<LoginResponse> {
    return this.http
      .post(this.url, request)
      .pipe(
        switchMap(() => this.loginService.login({ email: request.email, password: request.password })),
      );
  }
}
