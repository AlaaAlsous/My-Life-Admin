import { Injectable, signal } from '@angular/core';

const tokenStorageKey = 'my-life-admin.auth-token';
const nameStorageKey = 'my-life-admin.user-name';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private readonly loggedIn = signal(this.hasValidToken());

  readonly isLoggedIn = this.loggedIn.asReadonly();

  getToken(): string | null {
    const token = localStorage.getItem(tokenStorageKey);

    if (!token) {
      return null;
    }

    if (this.isExpired(token)) {
      this.clearToken();
      return null;
    }

    return token;
  }

  setToken(token: string, name?: string): void {
    if (!token.trim()) {
      throw new Error('JWT token is missing from login response.');
    }

    localStorage.setItem(tokenStorageKey, token);

    if (name && name.trim()) {
      localStorage.setItem(nameStorageKey, name.trim());
    }

    this.loggedIn.set(true);
  }

  clearToken(): void {
    localStorage.removeItem(tokenStorageKey);
    localStorage.removeItem(nameStorageKey);
    this.loggedIn.set(false);
  }

  getUserName(): string | null {
    const storedName = localStorage.getItem(nameStorageKey);

    if (storedName && storedName.trim()) {
      return storedName;
    }

    // Fallback for tokens that were stored before the name was persisted.
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payload = this.decodePayload(token);
      const name = payload['unique_name'] ?? payload['name'] ?? payload['email'];
      return typeof name === 'string' && name.trim() ? name : null;
    } catch {
      return null;
    }
  }

  private hasValidToken(): boolean {
    const token = localStorage.getItem(tokenStorageKey);
    return token !== null && !this.isExpired(token);
  }

  private isExpired(token: string): boolean {
    try {
      const payload = this.decodePayload(token);
      const exp = typeof payload['exp'] === 'number' ? payload['exp'] : null;
      return exp !== null && Date.now() >= exp * 1000;
    } catch {
      return true;
    }
  }

  private decodePayload(token: string): Record<string, unknown> {
    const parts = token.split('.');

    if (parts.length !== 3) {
      return {};
    }

    const body = parts[1]!;
    const base64 = body.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    const json = atob(padded);

    return JSON.parse(json) as Record<string, unknown>;
  }
}
