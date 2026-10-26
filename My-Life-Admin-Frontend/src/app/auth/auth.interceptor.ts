import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthTokenService } from './auth-token.service';

const backendBaseUrl = 'http://localhost:5076';
const authPathPrefix = `${backendBaseUrl}/api/auth/`;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenService = inject(AuthTokenService);
  const token = tokenService.getToken();
  const isBackendRequest = request.url.startsWith(backendBaseUrl);
  const isAuthRequest = request.url.startsWith(authPathPrefix);

  if (!token || !isBackendRequest || isAuthRequest) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    }),
  );
};
