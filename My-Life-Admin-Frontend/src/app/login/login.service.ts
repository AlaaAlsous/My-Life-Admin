import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { AuthTokenService } from '../auth/auth-token.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  id: number;
  name: string;
  email: string;
  token: string;
}

@Injectable({ providedIn: 'root' })
export class LoginService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(AuthTokenService);
  private readonly url = 'http://localhost:5076/api/auth/login';

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(this.url, request).pipe(
      map((response) => {
        this.tokenService.setToken(response.token);
        return response;
      }),
    );
  }
}
