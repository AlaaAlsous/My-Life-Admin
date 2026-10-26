import { Injectable } from '@angular/core';

const tokenStorageKey = 'my-life-admin.auth-token';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  getToken(): string | null {
    return localStorage.getItem(tokenStorageKey);
  }

  setToken(token: string): void {
    if (!token.trim()) {
      throw new Error('JWT token is missing from login response.');
    }

    localStorage.setItem(tokenStorageKey, token);
  }

  clearToken(): void {
    localStorage.removeItem(tokenStorageKey);
  }
}
