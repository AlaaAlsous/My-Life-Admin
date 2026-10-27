import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthTokenService } from './auth-token.service';

export const authGuard: CanActivateFn = () => {
  const tokenService = inject(AuthTokenService);
  return tokenService.getToken() ? true : inject(Router).createUrlTree(['/']);
};
