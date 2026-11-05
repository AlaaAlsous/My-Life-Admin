import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthTokenService } from './auth-token.service';
import { environment } from '../../environments/environment';

const backendBaseUrl = environment.apiUrl;
const authPathPrefix = `${backendBaseUrl}/api/auth/`;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenService = inject(AuthTokenService);
  const router = inject(Router);
  const token = tokenService.getToken();
  const isBackendRequest = request.url.startsWith(backendBaseUrl);
  const isAuthRequest = request.url.startsWith(authPathPrefix);

  const authenticatedRequest =
    token && isBackendRequest && !isAuthRequest
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  const response = next(authenticatedRequest);

  if (!isBackendRequest || isAuthRequest) {
    return response;
  }

  return response.pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        tokenService.clearToken();
        void router.navigateByUrl('/');
      }
      return throwError(() => error);
    }),
  );
};
