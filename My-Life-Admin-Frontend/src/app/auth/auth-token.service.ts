import { Injectable, signal } from '@angular/core';

const tokenStorageKey = 'my-life-admin.auth-token';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private readonly loggedIn = signal(this.getToken() !== null);

  readonly isLoggedIn = this.loggedIn.asReadonly();

  getToken(): string | null {
    return localStorage.getItem(tokenStorageKey);
  }

  setToken(token: string): void {
    if (!token.trim()) {
      throw new Error('JWT token is missing from login response.');
    }

    localStorage.setItem(tokenStorageKey, token);
    this.loggedIn.set(true);
  }

  clearToken(): void {
    localStorage.removeItem(tokenStorageKey);
    this.loggedIn.set(false);
  }
}
